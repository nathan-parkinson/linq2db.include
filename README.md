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
Simply add

``` c#
using LinqToDB.Include;
```
to the top of the file and then eager loading can be added to any linq2db `IQueryable<T>` object as shown below

``` c#
var query = from p in db.People
            where
                p.FirstName == "Billy"
            select p;

query = query.Include(x => x.Spouse);
```

`IEnumerables` can be added like this
``` c#
query = query.Include(x => x.Children.First());
```
or like this
``` c#
query = query.Include(x => x.Children[0]);
```

linq2db.include allows for nested includes, like this
``` c#
query = query.Include(x => x.Spouse.CurrentJob);
```

and through `IEnumerables` like this
``` c#
query = query.Include(x => x.Children[0].School);
```

or this

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

Extra filters can be added to nested items to limit the child entities that are loaded
``` c#
lobQuery.Include(x => x.Orders.First(), x => x.OrderQty > 10);
```


It is important that the first call to `.Include` method sets the result back to the variable that is going to be executed. This is because the Include method returns an object of type `IncludableQueryable` which is required for the eager loading to work.  It is this object that must be used to execute the query.

This is correct
``` c#
var query = from p in db.People
            where
                p.FirstName == "Billy"
            select p;

query = query.Include(x => x.Spouse);
var people = query.ToList();
```

This is incorrect

``` c#
var query = from p in db.People
            where
                p.FirstName == "Billy"
            select p;

query.Include(x => x.Spouse);
var people = query.ToList();
```

After the first call to `.Include` subsequent calls do not need to handle the return value, as show below

``` c#
var query = from p in db.People
            where
                p.FirstName == "Billy"
            select p;

query = query.Include(x => x.Spouse);
query.Include(x => x.CurrentJob);
var people = query.ToList();
```

## Predicates and Performance

linq2db allows for associations to be set via expressio predicates.  This is a very useful and powerful feature that allows for entity relationships to be defined by more than just a plain field = field expression.  

linq2db.include can handle associations defined in this way, however, there is a performance cost when there are no field = field conditions.  

linq2db.include uses these simple equals conditions to match the loaded entities and their child\parent entities.  A lookup is used which uses a key to dramaticaly improve performance.  Without any such simple condition linq2db.include falls back to a simple `.Where` method to find related entities which is conciderably slower.  Below are some fluent mapping examples to explain.  

Class definition
``` c#
public class Person
{
    public int PersonId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime Dob { get; set; }
    public List<Order> Orders { get; set; } = new List<Order>();
    public static Expression<Func<Person, Order, bool>> ExtraJoinOptions = (p, o) => o.OrderId < 99;
}
```


Fastest configuration

``` c#
builder.Entity<Person>()    
    .Property(x => x.PersonId).IsIdentity().IsPrimaryKey().IsNullable(false)
    .Property(x => x.FirstName).HasLength(100).IsNullable(false)
    .Property(x => x.LastName).HasLength(100).IsNullable(false)
    .Property(x => x.Dob).IsNullable(false)        
    .Property(x => x.Orders).IsNotColumn().Association(x => x.Orders, p => p.PersonId, o => o.First().PersonId);
```

Slightly slower

``` c#
builder.Entity<Person>()    
    .Property(x => x.PersonId).IsIdentity().IsPrimaryKey().IsNullable(false)
    .Property(x => x.FirstName).HasLength(100).IsNullable(false)
    .Property(x => x.LastName).HasLength(100).IsNullable(false)
    .Property(x => x.Dob).IsNullable(false)        
    .Property(x => x.Orders).IsNotColumn().HasAttribute(new AssociationAttribute { Predicate = personPredicate, ThisKey = nameof(Order.PersonId), OtherKey = nameof(Person.PersonId), CanBeNull = true });

```

Slowest
``` c#
builder.Entity<Person>()    
    .Association(x => x.Orders, (p, o) => p.PersonId == o.PersonId && o.OrderId < 99)
    .Property(x => x.PersonId).IsIdentity().IsPrimaryKey().IsNullable(false)
    .Property(x => x.FirstName).HasLength(100).IsNullable(false)
    .Property(x => x.LastName).HasLength(100).IsNullable(false)
    .Property(x => x.Dob).IsNullable(false)        
    .Property(x => x.Orders).IsNotColumn();

```

In summary, if predicates are used, it is better to keep any field = field conditions in the `ThisKey` other `OtherKey` properties of the `AssociationAttribute` and keep only non = conditions in the predicate expression.


## Roadmap

These are high level ideas at this stage and may or may not be possible\implemented.

* Complete custom override system to allow developer to insert custom code to load and match up child entities
* Add unit tests
* ThenInclude
* Automatically match up back references
* Async
* Cache compiled Func objects for better performance
* Experiment with ways to find equals conditions in ExpressionPredicates for improved performance
* For 1 to 1 relationships use a join instead of a separate query

## License

This project is licensed under the MIT License - see the [MIT-LICENSE.md](MIT-LICENSE.md) file for details
