using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.Utils
{
    static class EntityPropertySetter
    {
        internal static IList<TParent> SetField<TParent, TChild>(IList<TParent> parentEntities, IList<TChild> childEntities, EntityBuilderSchema schema)
            where TParent : class
            where TChild : class
        {            
            var childHasherExpression = CreateHashCodeExpression<TChild>(schema.ParentToChildAssociationDescriptor.OtherKey);
            var childHasher = childHasherExpression.Compile();
            var childLookup = childEntities.ToLookup(childHasher);

            var parentHasherExpression = CreateHashCodeExpression<TParent>(schema.ParentToChildAssociationDescriptor.ThisKey);
            var parentHasher = parentHasherExpression.Compile();

            if (schema.IsPropertyICollection)
            {
                var setter = schema.ParentType.CreateCollectionPropertySetter<TParent, TChild>(schema.PropertyName, schema.PropertyType);                
                var ifnullSetter = schema.ParentType.CreatePropertySetup<TParent, TChild>(schema.PropertyName);
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
                var setter = schema.ParentType.CreatePropertySetter<TParent, TChild>(schema.PropertyName);

                foreach (var item in parentEntities)
                {
                    var childEntity = childLookup[parentHasher(item)].FirstOrDefault();                    
                    setter(item, childEntity);
                }
            }
            

            return parentEntities;
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
            Type type = typeof(T);
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
