﻿using LinqToDB.Async;
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

namespace LinqToDB.Include
{
    public class IncludableQueryable<T, P> : IncludableQueryable<T>, IIncludableQueryable<T, P>
        where T : class
        where P : class
    {
        private readonly Expression<Func<T, P>> _previousPropertyExpr;

        internal IncludableQueryable(IQueryable<T> query)
            : base(query)
        { }


        internal IncludableQueryable(IQueryable<T> query, IRootAccessor<T> rootAccessor,
            Expression<Func<T, P>> previousPropertyExpression = null)
            : base(query, rootAccessor)
        {
            _previousPropertyExpr = previousPropertyExpression;
        }


        public IIncludableQueryable<T, TProperty> AddThenExpression<TProperty>(Expression<Func<P, TProperty>> expr,
            Expression<Func<TProperty, bool>> propertyFilter = null)
            where TProperty : class
        {

            Expression<Func<T, TProperty>> lambda = null;

            //implement code here to combine the previous expression with 
            if (_previousPropertyExpr != null)
            {
                var param = Expression.Parameter(typeof(T), "item");
                var item = Expression.Invoke(_previousPropertyExpr, param);
                var key = Expression.Invoke(expr, item);
                lambda = Expression.Lambda<Func<T, TProperty>>(key, param);
            }

            //var visitor = new PropertyVisitor<T>(_rootAccessor);
            //visitor.MapProperties(expr, propertyFilter);
            //return this;

            var newIncludable = new IncludableQueryable<T, TProperty>(LinqToDBQuery, _rootAccessor, lambda);
            newIncludable.AddExpression(lambda, propertyFilter);
            return newIncludable;
        }

        public IIncludableQueryable<T, P> AddExpression(Expression<Func<T, P>> expr,
            Expression<Func<P, bool>> propertyFilter = null)
        {
            var visitor = new PropertyVisitor<T>(_rootAccessor);
            visitor.MapProperties(expr, propertyFilter);
            return this;
        }
    }



    public class IncludableQueryable<T> : IIncludableQueryable<T> where T : class
    {
        internal readonly IRootAccessor<T> _rootAccessor;

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

        internal IncludableQueryable<T, P> ToPropertyIncludable<P>()
            where P : class
            => new IncludableQueryable<T, P>(LinqToDBQuery, _rootAccessor);




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
}
