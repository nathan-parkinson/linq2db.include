using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Include
{
    public static class IncludeExtensions
    {
        public static IIncludableQueryable<TClass> Include<TClass, TProperty>(
                this IQueryable<TClass> query, 
                Expression<Func<TClass, TProperty>> expr, 
                Expression<Func<TProperty, bool>> propertyFilter = null)
            where TClass : class
            where TProperty : class
                => new IncludableQueryable<TClass>(query).Include(expr, propertyFilter);

        public static IIncludableQueryable<TClass, TProperty> Include<TClass, TProperty>(
                this IIncludableQueryable<TClass> includable,
                Expression<Func<TClass, TProperty>> expr,
                Expression<Func<TProperty, bool>> propertyFilter = null)
            where TClass : class
            where TProperty : class
        {
            if (includable is IncludableQueryable<TClass> concrete)
            {
                return concrete.ToPropertyIncludable<TProperty>().AddThenExpression(expr, propertyFilter);
            }

            throw new ArgumentException($"Source object is not of type '{nameof(IncludableQueryable<TClass>)}'");            
        }

        public static IIncludableQueryable<TClass, TProperty> ThenInclude<TClass, TPreviousProperty, TProperty>(
                this IIncludableQueryable<TClass, TPreviousProperty> includable,
                Expression<Func<TClass, TProperty>> expr,
                Expression<Func<TProperty, bool>> propertyFilter = null)
            where TClass : class
            where TPreviousProperty : class
            where TProperty : class
        {
            if(includable is IncludableQueryable<TClass, TProperty> concrete)
            {
                return concrete.ToPropertyIncludable<TProperty>().AddThenExpression(expr, propertyFilter);
            }

            throw new ArgumentException($"Source object is not of type '{nameof(IncludableQueryable<TClass, TProperty>)}'");
        }
    }
}
