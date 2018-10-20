using LinqToDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Utils
{
    public static class IncludeExtensions
    {
        public static async Task<List<T>> ToListAsync<T, U>(this IQueryable<T> query, Expression<Func<T, U>> include, bool isSlowQuery = false) where T : class where U : class
        {
            //TODO
            //Implement iSSlowQuery
            //if(result.Count > 2000 || PK == Composite Key)
            //{ TempTable }
            //else
            //{ In function}
            
            
            var dbContext = query.GetDataContext<IDataContext, T>();
            var schema = GetPropertyParts(include, dbContext);

            var entities = await query.ToListAsync();
            //need to know the below for when we assign it values back to the parent entity
            var (childQuery, reusableQuery) = ChildEntityQueryBuilder.BuildQueryableForProperty(dbContext, query, include, schema);





            //////////////////// TEST ZONE
            //Generate Type For the ToLookup Part
            if (schema.ParentToChildAssociationDescriptor.ThisKey.Length == 1)
            {
                var childEntities = await childQuery.ToListAsync();
                entities = EntityPropertySetter.SetBySingleField(entities, childEntities, schema).ToList();
            }
            else
            {
                throw new NotImplementedException();
                var columnDict = schema.ParentEntityDescriptor.Columns.Where(c => schema.ParentToChildAssociationDescriptor.ThisKey.Contains(c.MemberName)).ToDictionary(x => x.MemberName, x => x.MemberType);
                Type newObjType = EntityTypeBuilder.CreateNewType(columnDict, schema.ParentType.Name + schema.PropertyName + "Loader");

            }

            return entities;
        }


        
        private static EntityBuilderSchema GetPropertyParts<TParent, TChild>(Expression<Func<TParent, TChild>> expr, IDataContext dataContext) where TParent : class where TChild : class
        {
            string propertyName = null;
            Type entityType = null;
            Type parentType = null;
            Type propertyType = null;

            if (expr.Body is MethodCallExpression)
            {
                var methodCall = expr.Body as MethodCallExpression;
                var propertyExpr = methodCall.Arguments.FirstOrDefault() as MemberExpression;

                if (propertyExpr == null)
                {
                    propertyExpr = methodCall.Object as MemberExpression;
                }

                propertyName = propertyExpr.Member.Name;
                entityType = typeof(TChild);
                propertyType = propertyExpr.Type;
                parentType = propertyExpr.Expression.Type;

            }
            else if (expr.Body is MemberExpression)
            {
                var propertyExpr = expr.Body as MemberExpression;

                propertyName = propertyExpr.Member.Name;
                entityType = propertyExpr.Type;
                propertyType = propertyExpr.Type;
                parentType = propertyExpr.Expression.Type;
            }


            var parentDesc = dataContext.MappingSchema.GetEntityDescriptor(parentType);
            var assoc = parentDesc.Associations.Where(x => x.MemberInfo.Name == propertyName).Single();


            return new EntityBuilderSchema
            {
                PropertyName = propertyName,
                ChildEntityType = entityType,
                PropertyType = propertyType,
                ParentType = parentType,

                IsPropertyICollection = propertyType.IsICollection(),
                IsPropertyIEnumerable = propertyType.IsIEnumerable(),
                IsChildEntityIEnumerable = entityType.IsIEnumerable(),

                ParentEntityDescriptor = parentDesc,
                ParentToChildAssociationDescriptor = assoc
            };
        }
    }

}
