using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Include.Setters
{
    interface ITypePoolDict<T> : ITypePool, IDictionary<int, T> where T : class
    {
        ILookup<int, T1> GetEntityLookupOfType<T1>(Func<T1, int> fkFunc) where T1 : class, T;
    }
}
