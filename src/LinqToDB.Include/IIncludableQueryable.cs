using LinqToDB.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Include
{
    public interface IIncludableQueryable<TClass> : IExpressionQuery<TClass> where TClass : class
    {
    }

    public interface IIncludableQueryable<TClass, TProperty> : IIncludableQueryable<TClass>
        where TClass : class
        where TProperty : class
    {

        IIncludableQueryable<TClass, TProperty> AddExpression(Expression<Func<TClass, TProperty>> expr,
            Expression<Func<TProperty, bool>> propertyFilter = null);


        IIncludableQueryable<TClass, TNewProperty> AddThenExpression<TNewProperty>(
            Expression<Func<TProperty, TNewProperty>> expr, 
            Expression<Func<TNewProperty, bool>> propertyFilter = null)
            where TNewProperty : class;
    }
}