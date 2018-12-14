﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.Include
{
    static class EntityPropertySetter
    {
        internal static void SetField<TParent, TChild>(this PropertyAccessor<TParent, TChild> schema, 
                IList<TParent> parentEntities, 
                IList<TChild> childEntities)
            where TParent : class
            where TChild : class
        {
            if (schema.AssociationDescriptor.ThisKey.Any())
            {
                SetFieldWhereKeysAreDefined(schema, parentEntities, childEntities);
                return;
            }
            
            SetFieldWhereKeysAreNotDefined(schema, parentEntities, childEntities);
        }


        private static void SetFieldWhereKeysAreNotDefined<TParent, TChild>(
                PropertyAccessor<TParent, TChild> schema, 
                IList<TParent> parentEntities, 
                IList<TChild> childEntities)
            where TParent : class
            where TChild : class
        {
            var predicate = schema.AssociationDescriptor.GetPredicate(typeof(TParent), typeof(TChild))
                as Expression<Func<TParent, TChild, bool>>;

            if (predicate == null)
            {
                predicate = (p, o) => true;
            }

            var predicateFunc = predicate.Compile();
            
            if (schema.IsMemberTypeICollection)
            {
                var setter = schema.DeclaringType.CreateCollectionPropertySetter<TParent, TChild>(schema.PropertyName, 
                    schema.MemberType);

                var ifnullSetter = schema.DeclaringType.CreatePropertySetup<TParent, TChild>(schema.PropertyName);

                foreach (var item in parentEntities)
                {
                    ifnullSetter(item);
                    foreach (var childEntity in childEntities.Where(x => predicateFunc(item, x)))                        
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
                    var childEntity = childEntities.FirstOrDefault(x => predicateFunc(item, x));                    
                    setter(item, childEntity);                    
                }
            }
        }



        private static void SetFieldWhereKeysAreDefined<TParent, TChild>(
                PropertyAccessor<TParent, TChild> schema, 
                IList<TParent> parentEntities, 
                IList<TChild> childEntities)
            where TParent : class
            where TChild : class
        {
            var predicate = schema.AssociationDescriptor.GetPredicate(typeof(TParent), typeof(TChild))
                as Expression<Func<TParent, TChild, bool>>;

            if(predicate == null)
            {
                predicate = (p, o) => true;
            }

            var predicateFunc = predicate.Compile();

            var childHasherExpression = CreateHashCodeExpression<TChild>(schema.AssociationDescriptor.OtherKey);
            var childHasher = childHasherExpression.Compile();
            var childLookup = childEntities.ToLookup(childHasher);

            var parentHasherExpression = CreateHashCodeExpression<TParent>(schema.AssociationDescriptor.ThisKey);
            var parentHasher = parentHasherExpression.Compile();

            if (schema.IsMemberTypeICollection)
            {
                var setter = schema.DeclaringType.CreateCollectionPropertySetter<TParent, TChild>(schema.PropertyName, 
                    schema.MemberType);

                var ifnullSetter = schema.DeclaringType.CreatePropertySetup<TParent, TChild>(schema.PropertyName);

                foreach (var item in parentEntities)
                {
                    ifnullSetter(item);

                    foreach (var childEntity in childLookup[parentHasher(item)].Where(x => predicateFunc(item, x)))
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
                    var childEntity = childLookup[parentHasher(item)]
                                            .Where(x => predicateFunc(item, x))
                                            .FirstOrDefault();

                    setter(item, childEntity);
                }
            }
        }

        private static int MakeHashCode<T>(int val, T property)
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
        
        private static Expression<Func<T, int>> CreateHashCodeExpression<T>(string[] propertyNames) where T : class
        {
            var param2 = Expression.Parameter(typeof(T), "p");
            Expression exp = Expression.Constant(17, typeof(int));
            var type = typeof(T);
            foreach (var propertyName in propertyNames)
            {
                var property = type.GetProperty(propertyName);
                exp = Expression.Call(typeof(EntityPropertySetter), nameof(MakeHashCode), new Type[] 
                {
                    property.PropertyType
                }, exp, Expression.Property(param2, property));
            }

            var func = Expression.Lambda<Func<T, int>>(exp, param2);
            return func;
        }
    }
}
