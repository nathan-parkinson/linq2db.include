using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LinqToDB.Include.Tests")]
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

        public static IIncludableQueryable<TClass> Include<TClass, TProperty>(
                this IIncludableQueryable<TClass> includable, 
                Expression<Func<TClass, TProperty>> expr, 
                Expression<Func<TProperty, bool>> propertyFilter = null)
            where TClass : class
            where TProperty : class
                => includable.AddExpression(expr, propertyFilter);


        public static IIncludableQueryable<TClass> ToIncludableQueryable<TClass>(this IQueryable<TClass> query) 
            where TClass : class
            => new IncludableQueryable<TClass>(query);

        /*
        public static List<TClass> ToList<TClass>(this IIncludableQueryable<TClass> includable)
            where TClass : class
        {
            var includeImpl = includable as IncludableQueryable<TClass>;
            if(includeImpl == null)
            {
                throw new ArgumentException("parameter includable must be of type IncludableQueryable<TClass>");                     
            }
            
            var entities = includeImpl.LoadEntityMap(includable);            
            return entities;
        }
        */
    }
}
