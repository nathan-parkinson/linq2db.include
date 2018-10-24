using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.Utils
{
    static class EntityPropertySetter
    {
        internal static IList<TParent> SetBySingleField<TParent, TChild>(IList<TParent> parentEntities, IList<TChild> childEntities, EntityBuilderSchema schema)
            where TParent : class
            where TChild : class
        {

            var paramChildren = Expression.Parameter(schema.ChildEntityType, "l");
            var paramParentType = Expression.Parameter(schema.ParentType, "x");

            Expression whereExpr = null;
            for (var i = 0; i < schema.ParentToChildAssociationDescriptor.ThisKey.Length; i++)
            {
                var currentExpr = Expression.Equal(Expression.Property(paramChildren, schema.ParentToChildAssociationDescriptor.OtherKey[0]), Expression.Property(paramParentType, schema.ParentToChildAssociationDescriptor.ThisKey[0]));
                if (whereExpr == null)
                {
                    whereExpr = currentExpr;
                }
                else
                {
                    whereExpr = Expression.AndAlso(whereExpr, currentExpr);
                }

            }

            var devLambda = Expression.Lambda<Func<TParent, TChild, bool>>(whereExpr, paramParentType, paramChildren);

            var whereFunc = devLambda.Compile();

            if (schema.IsPropertyICollection)
            {
                var setter = schema.ParentType.CreateCollectionPropertySetter<TParent, TChild>(schema.PropertyName, schema.PropertyType);

                foreach (var item in parentEntities)
                {
                    foreach (var childEntity in childEntities.Where(x => whereFunc(item, x)))
                    {
                        setter(item, childEntity);
                    }
                }
            }
            else if (schema.IsPropertyIEnumerable)
            {
                throw new NotImplementedException();
                /*
                var collectionSetter = schema.ParentType.CreateCollectionPropertySetter<TParent, TChild>(schema.PropertyName, typeof(ICollection<TChild>));                
                var propertySetter = schema.ParentType.CreatePropertySetter<TParent, TChild>(schema.PropertyName);
                foreach (var item in parentEntities)
                {
                    ICollection<TChild> itemCollection = new List<TChild>();
                    foreach (var childEntity in childEntities.Where(x => whereFunc(item, x)))
                    {
                        collectionSetter(item, childEntity);
                    }

                    propertySetter(item, itemCollection);                    
                }
                */
            }
            else
            {
                var setter = schema.ParentType.CreatePropertySetter<TParent, TChild>(schema.PropertyName);

                foreach (var item in parentEntities)
                {
                    var spouse = childEntities.FirstOrDefault(x => whereFunc(item, x));
                    setter(item, spouse);
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

        public static Expression<Func<T, int>> CreateHashCodeExpression<T>() where T : class
        {
            var param2 = Expression.Parameter(typeof(T), "p");
            Expression exp = Expression.Constant(17, typeof(int));

            foreach (var property in typeof(T).GetProperties())
            {
                exp = Expression.Call(typeof(EntityPropertySetter), nameof(MakeHashCode), new Type[] { property.PropertyType }, exp, Expression.Property(param2, property));
            }

            var func = Expression.Lambda<Func<T, int>>(exp, param2);
            return func;
        }
    }
}
