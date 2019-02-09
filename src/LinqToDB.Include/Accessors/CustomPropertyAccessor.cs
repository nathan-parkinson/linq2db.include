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
            Func<IQueryable<TClass>, Expression<Func<TProperty, bool>>, Expression<Func<TProperty, TProperty>>, List<TProperty>> queryExecuter,
            Func<IQueryable<TClass>, Expression<Func<TProperty, bool>>, IQueryable<TProperty>> reusableQueryBuilder)         
        {
            Key = memberInfoHashCode;            

            QueryExecuter = queryExecuter;
            ReusableQueryBuilder = reusableQueryBuilder;
        }
        
        public int Key { get; }

        public Func<IQueryable<TClass>, Expression<Func<TProperty, bool>>, Expression<Func<TProperty, TProperty>>, List<TProperty>> QueryExecuter { get; }
        public Func<IQueryable<TClass>, Expression<Func<TProperty, bool>>, IQueryable<TProperty>> ReusableQueryBuilder { get; }        
    }


    public class CustomTypeAccessor<TClass> : ICustomTypeAccessor where TClass : class    
    {
        public CustomTypeAccessor(Expression<Func<TClass, TClass>> entityBuilder)            
        {
            EntityBuilder = entityBuilder;
        }

        public Type Key { get; } = typeof(TClass);

        public Expression<Func<TClass, TClass>> EntityBuilder { get; }
    }
}
