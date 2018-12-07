using LinqToDB.Async;
using LinqToDB.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Utils
{
    public class IncludableQueryable<T> : IIncludableQueryable<T> where T : class
    {
        private readonly IRootAccessor<T> _rootAccessor;

        internal IncludableQueryable(IQueryable<T> query)
        {
            LinqToDBQuery = query as IExpressionQuery<T>;
            if (LinqToDBQuery == null)
            {
                throw new ArgumentException("IQueryable<T> must be of type LinqToDB.Linq.IExpressionQuery<T>");
            }

            var context = query.GetDataContext<IDataContext>();
            _rootAccessor = new RootAccessor<T>(context.MappingSchema);
        }

        internal IncludableQueryable(IQueryable<T> query, IRootAccessor<T> rootAccessor)
        {
            LinqToDBQuery = query as IExpressionQuery<T>;
            if (LinqToDBQuery == null)
            {
                throw new ArgumentException("IQueryable<T> must be of type LinqToDB.Linq.IExpressionQuery<T>");
            }

            var context = query.GetDataContext<IDataContext>();
            _rootAccessor = rootAccessor;
        }

        public IIncludableQueryable<T> AddExpression<TProperty>(Expression<Func<T, TProperty>> expr, Expression<Func<TProperty, bool>> propertyFilter = null)
            where TProperty : class
        {
            var visitor = new PropertyVisitor<T>(_rootAccessor);
            visitor.MapProperties(expr, propertyFilter);
            return this;
        }

        public IExpressionQuery<T> LinqToDBQuery { get; }

        public string SqlText => LinqToDBQuery.SqlText;

        IAsyncEnumerable<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression)
            => LinqToDBQuery.ExecuteAsync<TResult>(expression);

        async Task<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression, CancellationToken token)
            => await LinqToDBQuery.ExecuteAsync<TResult>(expression, token);


        public Type ElementType => LinqToDBQuery.ElementType;

        public Expression Expression => LinqToDBQuery.Expression;

        public IQueryProvider Provider => this;


        public IDataContext DataContext => LinqToDBQuery.DataContext;

        Expression IExpressionQuery<T>.Expression
        {
            get => LinqToDBQuery.Expression;
            set => LinqToDBQuery.Expression = value;
        }


        IQueryProvider IQueryable.Provider => this;
        Expression IQueryable.Expression => LinqToDBQuery.Expression;
        Type IQueryable.ElementType => LinqToDBQuery.ElementType;

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            var queryable = LinqToDBQuery.CreateQuery(expression) as IExpressionQuery<T>;
            return new IncludableQueryable<T>(queryable);
        }

        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
        {
            if (!typeof(TElement).IsClass)
            {
                //TODO Test this works
                return LinqToDB.Linq.Internals.CreateExpressionQueryInstance<TElement>(DataContext, Expression);
                //throw new ArgumentException("TElement must be a class");
            }

            var d1 = typeof(IncludableQueryable<>);
            Type[] typeArgs = { typeof(TElement) };
            var makeme = d1.MakeGenericType(typeArgs);

            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            CultureInfo culture = null; // use InvariantCulture or other if you prefer
            //object instantiatedType =
            //  Activator.CreateInstance(typeToInstantiate, flags, null, parameter, culture);

            object[] parameters = null;
            if (typeof(TElement) == typeof(T))
            {
                parameters = new object[]
                {
                    LinqToDBQuery.CreateQuery<TElement>(expression),
                    _rootAccessor
                };
            }
            else
            {
                parameters = new object[]
                {
                    LinqToDBQuery.CreateQuery<TElement>(expression)
                };
            }



            var returnQueryable = Activator.CreateInstance(makeme, flags, null, parameters, culture) 
                as IQueryable<TElement>;

            return returnQueryable;
        }


        object IQueryProvider.Execute(Expression expression) => LinqToDBQuery.Execute(expression);

        TResult IQueryProvider.Execute<TResult>(Expression expression)
            => LinqToDBQuery.Execute<TResult>(expression);
        

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            var entities = LinqToDBQuery.ToList();
            _rootAccessor.LoadMap(entities, this);

            return entities.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var entities = LinqToDBQuery.ToList();
            _rootAccessor.LoadMap(entities, this);

            return ((IEnumerable)entities).GetEnumerator();
        }
    }






    ////HOW TO DO ThenInclude now :-/
    //internal class IncludableQueryableOld<TClass> : IIncludableQueryable<TClass> where TClass : class
    //{
    //    private readonly IRootAccessor<TClass> _rootAccessor;

    //    internal IncludableQueryableOld(IQueryable<TClass> query)
    //    {
    //        var context = query.GetDataContext<IDataContext>();            
    //        _rootAccessor = new RootAccessor<TClass>(context.MappingSchema);
    //        Query = query;
    //    }

    //    public IQueryable<TClass> Query { get; }

    //    public IIncludableQueryable<TClass> AddExpression<TProperty>(Expression<Func<TClass, TProperty>> expr)
    //        where TProperty : class
    //    {
    //        var visitor = new PropertyVisitor<TClass>(_rootAccessor);
    //        visitor.MapProperties(expr);
    //        return this;
    //    }

    //    public List<TClass> LoadEntityMap(IIncludableQueryable<TClass> includable)
    //    {
    //        var query = includable.Query;
    //        var entities = query.ToList();
    //        _rootAccessor.LoadMap(entities, query);

    //        return entities;
    //    }
    //}



    public static class IncludeExtensions
    {
        public static IIncludableQueryable<TClass> Include<TClass, TProperty>(this IQueryable<TClass> query, Expression<Func<TClass, TProperty>> expr, Expression<Func<TProperty, bool>> propertyFilter = null)
            where TClass : class
            where TProperty : class
            => new IncludableQueryable<TClass>(query).Include(expr, propertyFilter);

        public static IIncludableQueryable<TClass> Include<TClass, TProperty>(this IIncludableQueryable<TClass> includable, Expression<Func<TClass, TProperty>> expr, Expression<Func<TProperty, bool>> propertyFilter = null)
            where TClass : class
            where TProperty : class
            => includable.AddExpression(expr, propertyFilter);

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
