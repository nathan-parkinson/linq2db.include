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


    }
}
