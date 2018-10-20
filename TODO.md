* Move to own project  
* match parent child entities
	*ToLookup for 

* When creating own type for lookup make sure it implements IEquatable<> so it can be indexed correctly
* Predicate \ ExpressionPredicate Or Self Build. Which to use and when

* Make Equal Expressions handle castable objects (e.g int? == int, int == long etc.)
* Handle IEnumerable<TChild> Property that is not ICollection<TChild>
* Handle ICollection<TChild> instantiation



* Implement ToLookup
* Implement isSlowQuery (In\TempTable)
