using LinqToDB;
using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.Utils
{
    static class ChildEntityQueryBuilder
    {      
        internal static (IQueryable<TChild> propertyQuery, IQueryable<TChild> reusableQuery) BuildQueryableForProperty<TParent, TChild>(IDataContext db, IQueryable<TParent> mainQuery, Expression<Func<TParent, TChild>> expr, EntityBuilderSchema schema) where TParent : class where TChild : class
        {        
            if (schema.IsChildEntityIEnumerable)
            {
                throw new ArgumentException($"'{nameof(TChild)}' cannot be an IEnumerable<>.");
            }

            var delegateType = typeof(Func<TChild, bool>);


            var mainQueryCopy = mainQuery;
            
            //join the query passed in to the child table
            var joinExpr = BuildJoinExpression<TParent, TChild>(schema.ParentToChildAssociationDescriptor, schema);
            var subSelect = db.GetTable<TChild>().Join(mainQueryCopy, SqlJoinType.Inner, joinExpr, (c, p) => c);

            //TODO subSelect can be kept and used for loading data nested deeper than ChildProperty

            var parentSet = subSelect;
            var parentCopy = db.GetTable<TChild>();

            var parentParam = Expression.Parameter(schema.ParentType, "parent1");
            var childParam1 = Expression.Parameter(schema.ChildEntityType, "child1");
            var childParam2 = Expression.Parameter(schema.ChildEntityType, "child2");

            //join the child table in the main section of the new to
            //the same table in the EXISTS function
            Expression innerCondition = BuildOuterWhereExpression(db.MappingSchema.GetEntityDescriptor(schema.ChildEntityType), childParam1, childParam2);



            // build theIQ.Where( pe2 => innerCondition )
            Type queryableType = typeof(Queryable);

            MethodCallExpression innerWhere = Expression.Call(
                queryableType,
                "Where",
                new Type[] { schema.ChildEntityType },
                new Expression[]
                {
                    parentSet.Expression,
                    Expression.Lambda(delegateType, innerCondition, new ParameterExpression[] { childParam2 })
                }
            );

            // build x.Where( pe2 => innerWhere ).Any()
            MethodCallExpression anyCall = Expression.Call(
                queryableType, "Any", new Type[] { schema.ChildEntityType }, innerWhere
            );

            // build x.Where( pe1 => anyCall )
            MethodCallExpression outerWhere = Expression.Call(
                queryableType,
                "Where",
                new Type[] { schema.ChildEntityType },
                new Expression[]
                {
                    parentCopy.Expression,
                    Expression.Lambda(delegateType, anyCall, new ParameterExpression[] { childParam1 })
                }
            );

            var resultingQuery = LinqToDB.Linq.Internals.CreateExpressionQueryInstance<TChild>(db, outerWhere);

            return (resultingQuery, subSelect);
        }

        private static Expression BuildOuterWhereExpression(EntityDescriptor childDesc, ParameterExpression childParam1, ParameterExpression childParam2)
        {
            var pkColumns = childDesc.Columns.Where(x => x.IsPrimaryKey);
            if (!pkColumns.Any())
            {
                throw new Exception($"No primary key defined for type '{childDesc.TypeAccessor}'");
            }

            Expression innerCondition = null;
            foreach (var col in childDesc.Columns.Where(x => x.IsPrimaryKey))
            {
                // pe1.TId
                Expression pe1TIdProp = Expression.Property(childParam1, col.MemberName);
                // pe2.TId
                Expression pe2TIdProp = Expression.Property(childParam2, col.MemberName);
                // build pe1.TId == pe2.TId
                Expression TIdEquals = Expression.Equal(pe1TIdProp, pe2TIdProp);

                if (innerCondition == null)
                {
                    innerCondition = TIdEquals;
                }
                else
                {
                    innerCondition = Expression.AndAlso(innerCondition, TIdEquals);
                }
            }

            return innerCondition;
        }

        private static Expression<Func<TChild, TParent, bool>> BuildJoinExpression<TParent, TChild>(AssociationDescriptor assoc, EntityBuilderSchema schema)
        {            
            ParameterExpression childParam = Expression.Parameter(schema.ChildEntityType, "c");
            ParameterExpression parentParam = Expression.Parameter(schema.ParentType, "p");

            BinaryExpression previousExpr = null;
            for (int i = 0; i < assoc.ThisKey.Length; i++)
            {
                Expression childProperty = Expression.Property(childParam, assoc.OtherKey[i]);
                Expression parentProperty = Expression.Property(parentParam, assoc.ThisKey[i]);

                if(childProperty.Type != parentProperty.Type)
                {
                    parentProperty = Expression.Convert(parentProperty, childProperty.Type);
                }

                var equals = Expression.Equal(childProperty, parentProperty);
                if (previousExpr == null)
                {
                    previousExpr = equals;
                }
                else
                {
                    previousExpr = Expression.AndAlso(previousExpr, equals);
                }
            }

            /*
             * TODO test this code and check whether ExpressionPredicate or Predicate are the correct properties to use here
            if (!string.IsNullOrEmpty(assoc.ExpressionPredicate))
            {
                var expressionPredicate = parentType.GetProperty(assoc.ExpressionPredicate).GetValue(null, null) as Expression<Func<TParent, TChild, bool>>;
                if(expressionPredicate != null)
                {
                    previousExpr = Expression.AndAlso(previousExpr, expressionPredicate);
                }
            }
            */

            var expr = Expression.Lambda<Func<TChild, TParent, bool>>(previousExpr, childParam, parentParam);
            return expr;
        }

    }

}
