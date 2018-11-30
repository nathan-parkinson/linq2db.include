using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Utils
{
    //HOW TO DO ThenInclude now :-/
    internal class IncludableQueryable<TClass> : IIncludableQueryable<TClass> where TClass : class
    {
        private readonly IRootAccessor<TClass> _rootAccessor;

        internal IncludableQueryable(IQueryable<TClass> query)
        {
            var context = query.GetDataContext<IDataContext>();            
            _rootAccessor = new RootAccessor<TClass>(context.MappingSchema);
            Query = query;
        }

        public IQueryable<TClass> Query { get; }

        public IIncludableQueryable<TClass> AddExpression<TProperty>(Expression<Func<TClass, TProperty>> expr)
            where TProperty : class
        {
            var visitor = new PropertyVisitor<TClass>(_rootAccessor);
            visitor.MapProperties(expr);
            return this;
        }
        
        public List<TClass> LoadEntityMap(IIncludableQueryable<TClass> includable)
        {
            var query = includable.Query;
            var entities = query.ToList();
            _rootAccessor.LoadMap(entities, query);

            return entities;
        }
    }



    public static class IncludeExtensions2
    {
        public static IIncludableQueryable<TClass> Include<TClass, TProperty>(this IQueryable<TClass> query, Expression<Func<TClass, TProperty>> expr)
            where TClass : class
            where TProperty : class
            => new IncludableQueryable<TClass>(query).Include(expr);

        public static IIncludableQueryable<TClass> Include<TClass, TProperty>(this IIncludableQueryable<TClass> includable, Expression<Func<TClass, TProperty>> expr)
            where TClass : class
            where TProperty : class
            => includable.AddExpression(expr);

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
    }
}
