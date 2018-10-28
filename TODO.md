



* Make Equal Expressions handle castable objects (e.g int? == int, int == long etc.)
* Add ExpressionPredicate\Predicate to joins
	Need to figure out the difference between these 2 properties

* Implement Include\ThenInclude
* Make entire querybuilder into a compiled func so different queries can use the same func
* Store comiled funcs in a cache (ConcurrentDictionary)
	Use AssociationDescriptor.GetHashCode() as the Key
* Allow for inheritance in Include functions (e.g. Include(x => (LetTransaction)x.Transactions) or Include(x => x.Transactions).OfType<LetTransaction>())



* Implement isSlowQuery (In\TempTable)
* Add optional Filter Expression to Include functionality





	