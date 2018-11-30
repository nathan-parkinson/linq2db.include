using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Utils
{
    public interface IIncludableQueryable<TClass> where TClass : class
    {
        IQueryable<TClass> Query { get; }
        IIncludableQueryable<TClass> AddExpression<TProperty>(Expression<Func<TClass, TProperty>> expr) where TProperty : class;
        List<TClass> LoadEntityMap(IIncludableQueryable<TClass> includable);
    }
}