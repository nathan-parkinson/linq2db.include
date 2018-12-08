using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.Utils
{
    public static class EntityMapOverride
    {
        internal static readonly ConcurrentDictionary<int, ICustomPropertyAccessor> CustomOverrides
            = new ConcurrentDictionary<int, ICustomPropertyAccessor>();
        
        internal static CustomPropertyAccessor<TClass, TProperty> Get<TClass, TProperty>(int memberInfoHashCode)
            where TClass : class
            where TProperty : class
        {
            if (CustomOverrides.TryGetValue(memberInfoHashCode, out ICustomPropertyAccessor schema))
            {
                return schema as CustomPropertyAccessor<TClass, TProperty>;
            }

            return null;
        }

        public static bool Set<TClass, TProperty>(Expression<Func<TClass, TProperty>> expr,
                Func<IQueryable<TClass>, Expression<Func<TProperty, bool>>, List<TProperty>> queryExecuter,
                Func<IQueryable<TClass>, Expression<Func<TProperty, bool>>, IQueryable<TProperty>> reusableQueryBuilder)
            where TClass : class
            where TProperty : class
        {
            var memberInfo = PathWalker.GetMembers(expr).LastOrDefault();

            if(memberInfo == null)
            {
                return false;
            }

            var cpa = new CustomPropertyAccessor<TClass, TProperty>(
                memberInfo.GetHashCode(),
                queryExecuter,
                reusableQueryBuilder);
            
            return CustomOverrides.TryAdd(cpa.Key, cpa);
        }

    }
}
