using LinqToDB.Include.Cache;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.Include
{
    static class EntityPropertySetter
    {
        private readonly static ConcurrentDictionary<FKHashFuncKey, IFKHashCodeFunc> _fkHashFuncs
            = new ConcurrentDictionary<FKHashFuncKey, IFKHashCodeFunc>();


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

            var keyTuple = EntityMatchWalker.ExtractKeyNodes(predicate,
                    predicate.Parameters[0],
                    predicate.Parameters[1]);

            var thisKeyExpressions = keyTuple.Item1;
            var otherKeyExpressions = keyTuple.Item2;

            var otherKeys = KeyExpressionsToKeyHolder(schema.AssociationDescriptor.OtherKey, otherKeyExpressions);
            var thisKeys = KeyExpressionsToKeyHolder(schema.AssociationDescriptor.ThisKey, thisKeyExpressions);

            
            if (otherKeys.Any() && thisKeys.Any())
            {
                var hasher = _fkHashFuncs.GetOrAdd(new FKHashFuncKey
                {
                    OtherType = typeof(TChild),
                    OtherKeys = otherKeys.ToList(),
                    ThisType = typeof(TParent),
                    ThisKeys = thisKeys.ToList()
                }, k => new FKHashCodeFunc<TParent, TChild>(CreateHashCodeExpression<TParent>(thisKeys).Compile(),
                                    CreateHashCodeExpression<TChild>(otherKeys).Compile(),
                                    predicate.Compile()));

                var typeHasher = hasher as FKHashCodeFunc<TParent, TChild>;

                var childLookup = childEntities.ToLookup(typeHasher.OtherHashCodeFunc);

                MatchEntityLookup(schema, parentEntities, typeHasher.AssociationPredicate, childLookup, typeHasher.ThisHashCodeFunc);
            }
            else
            {
                MatchEntityList(schema, parentEntities, childEntities, predicate.Compile());
            }
        }

        private static List<KeyHolder> KeyExpressionsToKeyHolder(string[] keys, List<Expression> keyExpressions)
        {
            return keyExpressions.OfType<ConstantExpression>().Select(x => new KeyHolder
            {
                Constant = x,
                Type = KeyType.Constant
            })
            .Union(
                keyExpressions.OfType<MemberExpression>().Select(x => new KeyHolder
                {
                    Type = KeyType.Property,
                    Key = x.Member.Name
                })
            ).Union(
                keys.Select(x => new KeyHolder
                {
                    Type = KeyType.Property,
                    Key = x
                })
            ).Distinct()
             .ToList();
        }

        private static void MatchEntityLookup<TParent, TChild>(PropertyAccessor<TParent, TChild> schema, 
                IList<TParent> parentEntities, 
                Func<TParent, TChild, bool> predicateFunc, 
                ILookup<int, TChild> childLookup, 
                Func<TParent, int> parentHasher)
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
