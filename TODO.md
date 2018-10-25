* Move to own project  
* Make Equal Expressions handle castable objects (e.g int? == int, int == long etc.)

* Handle ICollection<TChild> instantiation
* Can IsICollection be more efficient
* Work out the type of object to pass into ifnullSetter




* Implement isSlowQuery (In\TempTable)


* Is Queryable<TCHild> WHERE ANY(Queryable<TParent> JOIN Queryable<TChild>)
	stll the best thing to do?
	is it now better to have different queries depending on 
		* normal
		* slow\in
		* slow\temp table

* Add optional Filter Expression to Include functionality


	