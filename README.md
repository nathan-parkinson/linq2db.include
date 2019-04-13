# linq2db.include

linq2db.include adds Entity Framework style Include (eager loading) to linq2db

## Getting Started

From **NuGet**:
* `Install-Package linq2db.include -IncludePrerelease` - .NET & .NET Core

## .net platforms

linq2db.include works on .net standard 2.0, .net core 2.0 and .net framework 4.5 and above


Explain how to run the automated tests for this system

## How to use

linq2db.include uses the existing linq2db mappings - specifically the association mappings - to work the relationship between entities.  To use linq2db.include simply ensure that the associations are mapped in the `IDataContext`.


linq2db.include adds include functionality through extension methods.  
Simply add the line below to the top of the file.

``` c#
using LinqToDB.Include;
```

Then eager loading can be added to any linq2db `IQueryable<T>` object as shown below.
``` c#
var query = from p in db.People
            where
                p.FirstName == "Billy"
            select p;

query = query.Include(x => x.Spouse);
```

`IEnumerables` can be added like this.
``` c#
query = query.Include(x => x.Children.First());
```
Or like this.
``` c#
query = query.Include(x => x.Children[0]);
```

linq2db.include allows for nested includes, like this.
``` c#
query = query.Include(x => x.Spouse.CurrentJob);
```

And through `IEnumerable` members like this.
``` c#
query = query.Include(x => x.Children[0].School);
```

Or this.

``` c#
query = query.Include(x => x.Children.First().School);
```

### Inheritance

linq2db.include can handle inherited types and their nested properties. Properties of inherited types can be eager loaded as shown below.
``` c#
lobQuery.Include(x => x.Orders[0].ProductLines.OfType<ExtendedProductLine>().First().PropertyOfExtendedClass);
```

or
``` c#
productLineQuery.Include(x => ((ExtendedProductLine)x).PropertyOfExtendedClass);
```

### Filters

Extra filters can be added to nested items to limit the child entities that are loaded.
``` c#
lobQuery.Include(x => x.Orders.First(), x => x.OrderQty > 10);
```


It is important that the first call to `.Include` method sets the result back to the variable that is going to be executed. This is because the Include method returns an object of type `IncludableQueryable` which is required for the eager loading to work.  It is this object that must be used to execute the query.

This is correct.
``` c#
var query = from p in db.People
            where
                p.FirstName == "Billy"
            select p;

query = query.Include(x => x.Spouse);
var people = query.ToList();
```

This is incorrect.
``` c#
var query = from p in db.People
            where
                p.FirstName == "Billy"
            select p;

query.Include(x => x.Spouse);
var people = query.ToList();
```

After the first call to `.Include` subsequent calls do not need to handle the return value, as show below.

``` c#
var query = from p in db.People
            where
                p.FirstName == "Billy"
            select p;

query = query.Include(x => x.Spouse);
query.Include(x => x.CurrentJob);
var people = query.ToList();
```

## Composing Queries

Queries can still be composed as they normally would be using linq2db.  
This will retain the include config.
``` c#
var query = from p in db.People
            where
                p.DOB > DateTime.Now.AddYears(-10)
            select p;

query = query.Include(p => p.Children.First());

query = from p in query
        where
            p.LastName = "Smith"
        select p;
```

This will not.
``` c#
var query = from p in db.People
            where
                p.DOB > DateTime.Now.AddYears(-10)
            select p;

query = query.Include(p => p.Children.First());

var jobQuery  = from p in query
                where
                    p.LastName = "Smith"
                select p.CurrentJob;
```
## Roadmap

These are high level ideas at this stage and may or may not be possible\implemented.

* Automatically match up back references
* Complete custom override system to allow developer to insert custom code to load and match up child entities
* Add more unit tests
* ThenInclude

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
