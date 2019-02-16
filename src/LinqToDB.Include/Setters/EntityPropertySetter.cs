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


            //TODO, improve below query, perhaps with IEquatable class
            //Can ThisKey and OtherKey be null
            var otherKeys = otherKeyExpressions.OfType<ConstantExpression>().Select(x => new
            {
                Constant = x,
                Type = KeyType.Constant,
                Key = default(string)
            })
            .Union(
                otherKeyExpressions.OfType<MemberExpression>().Select(x => new
                {
                    Constant = default(ConstantExpression),
                    Type = KeyType.Property,
                    Key = x.Member.Name
                })
            ).Union(
                schema.AssociationDescriptor.OtherKey.Select(x => new
                {
                    Constant = default(ConstantExpression),
                    Type = KeyType.Property,
                    Key = x
                })
            ).Distinct().Select(x => new KeyHolder
            {
                Constant = x.Constant,
                Key = x.Key,
                Type = x.Type
            }).ToList();




            var thisKeys = thisKeyExpressions.OfType<ConstantExpression>().Select(x => new
            {
                Constant = x,
                Type = KeyType.Constant,
                Key = default(string)
            })
            .Union(
                thisKeyExpressions.OfType<MemberExpression>().Select(x => new
                {
                    Constant = default(ConstantExpression),
                    Type = KeyType.Property,
                    Key = x.Member.Name
                })
            ).Union(
                schema.AssociationDescriptor.ThisKey.Select(x => new
                {
                    Constant = default(ConstantExpression),
                    Type = KeyType.Property,
                    Key = x
                })
            ).Distinct().Select(x => new KeyHolder
            {
                Constant = x.Constant,
                Key = x.Key,
                Type = x.Type
            }).ToList();





            var childHasherExpression = CreateHashCodeExpression<TChild>(otherKeys);
            var childHasher = childHasherExpression.Compile();
            var childLookup = childEntities.ToLookup(childHasher);

            var parentHasherExpression = CreateHashCodeExpression<TParent>(thisKeys);
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

        private static Expression<Func<T, int>> CreateHashCodeExpressionOld<T>(string[] propertyNames) where T : class
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

        private class KeyHolder
        {
            public string Key { get; set; }
            public KeyType Type { get; set; }
            public ConstantExpression Constant { get; set; }
        }

        private enum KeyType
        {
            Property = 0,
            Constant = 1
        }

        private static Expression<Func<T, int>> CreateHashCodeExpression<T>(IEnumerable<KeyHolder> propertyNames) where T : class
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
