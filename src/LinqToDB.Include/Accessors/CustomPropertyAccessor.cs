using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Include
{
    public class CustomPropertyAccessor<TClass, TProperty> : ICustomPropertyAccessor
        where TClass : class
        where TProperty : class
    {
        public CustomPropertyAccessor(
            int memberInfoHashCode,
            Func<IQueryable<TClass>, Expression<Func<TProperty, bool>>, List<TProperty>> queryExecuter,
            Func<IQueryable<TClass>, Expression<Func<TProperty, bool>>, IQueryable<TProperty>> reusableQueryBuilder)
        {
            Key = memberInfoHashCode;
            QueryExecuter = queryExecuter;
            ReusableQueryBuilder = reusableQueryBuilder;
        }

        public Func<IQueryable<TClass>, Expression<Func<TProperty, bool>>, List<TProperty>> QueryExecuter { get; }
        public Func<IQueryable<TClass>, Expression<Func<TProperty, bool>>, IQueryable<TProperty>> ReusableQueryBuilder { get; }

        public int Key { get; }
    }
}
