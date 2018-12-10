* Implement ThenInclude
* Automatically handle back-references
* Implement isSlowQuery (In\TempTable)
* Async
* Add Unit Testing
* Make entire querybuilder into a compiled func so different queries can use the same func
* Store compiled funcs in a cache (ConcurrentDictionary)
	Use AssociationDescriptor.GetHashCode() as the Key

* All deleting and\or clearing of items in accessor override ConcurrentDictionary
* Improve setter perfomance when using predicate expression to match parent\child entities

* Test CustomAccessors
* Create AccessorNotFoundException


Things to test
	Nested Loading
	Nested Loading of inherited types at nested and root level
	Joining different types
	Joining on single keys
	Joining on composite keys
	Joining on predicate
	Setting values based on primary key
	Setting values based on predicate
	Setting values based on mixture of primary key and predicate
	Overriding accessors at root level
	Overriding nested accessor
	Overriding accessor of inherited type
	Multiple includes of same member does not create dupe accessors
	IncludableQueryable is composable
	Changing TClass of IncludabelQueryable clears root accessor


create new git repo from this one, using only the files in the LinqToFB.Utils directory
rename to LinqToDB.Include
create a new solution (sln) for this
add unit test project
upload to github
add readme.md
add wiki
add to nuget (pre-release)