using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.Include
{
    public static class EntityMapOverride
    {
        private static readonly ConcurrentDictionary<int, ICustomPropertyAccessor> CustomPropertyOverrides
            = new ConcurrentDictionary<int, ICustomPropertyAccessor>();

        private static readonly ConcurrentDictionary<Type, ICustomTypeAccessor> CustomTypeOverrides
            = new ConcurrentDictionary<Type, ICustomTypeAccessor>();

        internal static CustomPropertyAccessor<TClass, TProperty> Get<TClass, TProperty>(int memberInfoHashCode)
            where TClass : class
            where TProperty : class
        {
            if (CustomPropertyOverrides.TryGetValue(memberInfoHashCode, out ICustomPropertyAccessor schema))
            {
                return schema as CustomPropertyAccessor<TClass, TProperty>;
            }

            return null;
        }

        public static bool Set<TClass, TProperty>(Expression<Func<TClass, TProperty>> expr,
                Func<IQueryable<TClass>, Expression<Func<TProperty, bool>>, Expression<Func<TProperty, TProperty>>, List<TProperty>> queryExecuter,
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
            
            return CustomPropertyOverrides.TryAdd(cpa.Key, cpa);
        }


        internal static Expression<Func<TClass, TClass>> Get<TClass>()
            where TClass : class
        {
            if (CustomTypeOverrides.TryGetValue(typeof(TClass), out ICustomTypeAccessor schema))
            {
                if(schema is CustomTypeAccessor<TClass> typeAccessor)
                {
                    return typeAccessor.EntityBuilder;
                }
            }

            return null;
        }

        public static bool Set<TClass>(Expression<Func<TClass, TClass>> entityBuilder)
            where TClass : class
        {            
            if(entityBuilder == null)
            {
                //allow setting back to null to remove a custom action we have added previously
                return CustomTypeOverrides.TryAdd(typeof(TClass), null);
            }

            var cpa = new CustomTypeAccessor<TClass>(entityBuilder);
            return CustomTypeOverrides.TryAdd(cpa.Key, cpa);
        }

    }
}
