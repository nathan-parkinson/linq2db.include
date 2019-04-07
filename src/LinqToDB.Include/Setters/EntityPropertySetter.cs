using System;
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
            SetFieldByKeys(schema, parentEntities, childEntities);
        }

        


        private static void SetFieldByKeys<TParent, TChild>(
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
            var keyTuple = EntityMatchWalker.ExtractKeyNodes(predicate,
                    predicate.Parameters[0],
                    predicate.Parameters[1]);

            var thisKeyExpressions = keyTuple.Item1;
            var otherKeyExpressions = keyTuple.Item2;


            var otherKeys = otherKeyExpressions.OfType<ConstantExpression>().Select(x => new KeyHolder
            {
                Constant = x,
                Type = KeyType.Constant
            })
            .Union(
                otherKeyExpressions.OfType<MemberExpression>().Select(x => new KeyHolder
                {
                    Type = KeyType.Property,
                    Key = x.Member.Name
                })
            ).Union(
                schema.AssociationDescriptor.OtherKey.Select(x => new KeyHolder
                {
                    Type = KeyType.Property,
                    Key = x
                })
            ).Distinct()
             .ToList();




            var thisKeys = thisKeyExpressions.OfType<ConstantExpression>().Select(x => new KeyHolder
            {
                Constant = x,
                Type = KeyType.Constant
            })
            .Union(
                thisKeyExpressions.OfType<MemberExpression>().Select(x => new KeyHolder
                {
                    Type = KeyType.Property,
                    Key = x.Member.Name
                })
            ).Union(
                schema.AssociationDescriptor.ThisKey.Select(x => new KeyHolder
                {
                    Type = KeyType.Property,
                    Key = x
                })
            ).Distinct()
             .ToList();


            if (otherKeys.Any() && thisKeys.Any())
            {
                var childHasherExpression = CreateHashCodeExpression<TChild>(otherKeys);
                var childHasher = childHasherExpression.Compile();
                var childLookup = childEntities.ToLookup(childHasher);

                var parentHasherExpression = CreateHashCodeExpression<TParent>(thisKeys);
                var parentHasher = parentHasherExpression.Compile();

                MatchEntityLookup(schema, parentEntities, predicateFunc, childLookup, parentHasher);
            }
            else
            {
                MatchEntityList(schema, parentEntities, childEntities, predicateFunc);
            }
        }

        private static void MatchEntityLookup<TParent, TChild>(PropertyAccessor<TParent, TChild> schema, IList<TParent> parentEntities, Func<TParent, TChild, bool> predicateFunc, ILookup<int, TChild> childLookup, Func<TParent, int> parentHasher)
            where TParent : class
            where TChild : class
        {
            if (schema.IsMemberTypeIEnumerable)
            {
                var setter = schema.DeclaringType.CreateCollectionPropertySetter<TParent, TChild>(schema.PropertyName,
                    schema.MemberType);

                var ifnullSetter = schema.DeclaringType.CreatePropertySetup<TParent, TChild>(schema.PropertyName);

                foreach (var item in parentEntities)
                {
                    ifnullSetter(item);

                    foreach (var childEntity in childLookup[parentHasher(item)]
                                                    .Where(x => predicateFunc(item, x)))
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

        private static void MatchEntityList<TParent, TChild>(PropertyAccessor<TParent, TChild> schema, IList<TParent> parentEntities, IList<TChild> childEntities, Func<TParent, TChild, bool> predicateFunc)
            where TParent : class
            where TChild : class
        {
            if (schema.IsMemberTypeIEnumerable)
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
        
        private class KeyHolder : IEquatable<KeyHolder>
        {
            public string Key { get; set; }
            public KeyType Type { get; set; }
            public ConstantExpression Constant { get; set; }

            public bool Equals(KeyHolder other)
            {
                return other != null && Key == other.Key && Type == other.Type && Constant?.Value == other.Constant?.Value;                
            }
            
            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = 17;
                    // Suitable nullity checks etc, of course :)
                    hash = hash * 23 + Type.GetHashCode();
                    if (Key != null)
                    {
                        hash = hash * 23 + Key.GetHashCode();
                    }

                    if(Constant != null)
                    {
                        hash = hash * 23 + Constant.Value.GetHashCode();
                    }

                    return hash;
                }
            }
        }

        private enum KeyType
        {
            Property = 0,
            Constant = 1
        }

        internal static Expression<Func<T, int>> CreateHashCodeExpression<T>(IEnumerable<string> propertyNames) 
            where T : class
        {
            return CreateHashCodeExpression<T>(propertyNames.Select(x => new KeyHolder
            {
                Type = KeyType.Property,
                Key = x
            }));
        }

        private static Expression<Func<T, int>> CreateHashCodeExpression<T>(IEnumerable<KeyHolder> propertyNames) 
            where T : class
        {
            var param2 = Expression.Parameter(typeof(T), "p");
            Expression exp = Expression.Constant(17, typeof(int));
            var type = typeof(T);
            foreach (var prop in propertyNames)
            {

                switch (prop.Type)
                {
                    case KeyType.Property:

                        var property = type.GetProperty(prop.Key);
                        exp = Expression.Call(typeof(EntityPropertySetter), nameof(MakeHashCode), new Type[]
                        {
                            property.PropertyType
                        }, exp, Expression.Property(param2, property));
                        break;
                    case KeyType.Constant:

                        exp = Expression.Call(typeof(EntityPropertySetter), nameof(MakeHashCode), new Type[]
                        {
                            prop.Constant.Type
                        }, exp, prop.Constant);
                        break;
                    default:
                        break;
                }
            }

            var func = Expression.Lambda<Func<T, int>>(exp, param2);
            return func;
        }
    }
}
