using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.Utils
{
    static class EntityPropertySetter2
    {
        internal static void SetField<TParent, TChild>(this PropertyAccessor<TParent, TChild> schema, IList<TParent> parentEntities, IList<TChild> childEntities)
            where TParent : class
            where TChild : class
        {                        
            var childHasherExpression = CreateHashCodeExpression<TChild>(schema.AssociationDescriptor.OtherKey);
            var childHasher = childHasherExpression.Compile();
            var childLookup = childEntities.ToLookup(childHasher);

            var parentHasherExpression = CreateHashCodeExpression<TParent>(schema.AssociationDescriptor.ThisKey);
            var parentHasher = parentHasherExpression.Compile();

            if (schema.IsMemberTypeICollection)
            {
                var setter = schema.DeclaringType.CreateCollectionPropertySetter<TParent, TChild>(schema.PropertyName, schema.MemberType);                
                var ifnullSetter = schema.DeclaringType.CreatePropertySetup<TParent, TChild>(schema.PropertyName);
                foreach (var item in parentEntities)
                {                    
                    ifnullSetter(item);

                    foreach (var childEntity in childLookup[parentHasher(item)])
                    {
                        setter(item, childEntity);
                    }
                }
            }            
            else
            {
                var setter = schema.DeclaringType.CreatePropertySetter<TParent, TChild>(schema.PropertyName);

                foreach (var item in parentEntities)
                {
                    var childEntity = childLookup[parentHasher(item)].FirstOrDefault();                    
                    setter(item, childEntity);
                }
            }            
        }
        
        public static int MakeHashCode<T>(int val, T property)
        {
            if (property != null)
            {
                unchecked
                {
                    val = val * 23 + property.GetHashCode();
                }
            }

            return val;
        }
        
        public static Expression<Func<T, int>> CreateHashCodeExpression<T>(string[] propertyNames) where T : class
        {
            var param2 = Expression.Parameter(typeof(T), "p");
            Expression exp = Expression.Constant(17, typeof(int));
            var type = typeof(T);
            foreach (var propertyName in propertyNames)
            {
                var property = type.GetProperty(propertyName);
                exp = Expression.Call(typeof(EntityPropertySetter), nameof(MakeHashCode), new Type[] { property.PropertyType }, exp, Expression.Property(param2, property));
            }

            var func = Expression.Lambda<Func<T, int>>(exp, param2);
            return func;
        }
    }
}
