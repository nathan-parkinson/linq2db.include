
* Allow for overriding of PropertyAccessor for manual mapping
* Automatically handle back-references
* Async
* Add Unit Testing
* Implement ThenInclude
* Make entire querybuilder into a compiled func so different queries can use the same func
* Store comiled funcs in a cache (ConcurrentDictionary)
	Use AssociationDescriptor.GetHashCode() as the Key


* Improve setter perfomance when using predicate expression to match parent\child entities
* Implement isSlowQuery (In\TempTable)



Overriding...
RootAccessor.GetByPath
	Add a check in here to a ConcurrentDictionary of Custom PropertyAccessors
	Make sure code checks for a custom implementation in the LoadMap, Load and MapProperties methods
	so there is not a cast exception


	Load Method
	Move the code below to a separate method
	```c#
	var propertyQuery = PropertyQueryBuilder.BuildQueryableForProperty(query, this);
            if (propertyFilter != null)
            {
                propertyQuery = propertyQuery.Where(propertyFilter);
            }


            //run query into list
            var propertyEntities = propertyQuery.ToList();

	```
	In that method check cache for a Func to handle it.  If Func exists, use it else execute code as normal

	Do the same for the reuseable query