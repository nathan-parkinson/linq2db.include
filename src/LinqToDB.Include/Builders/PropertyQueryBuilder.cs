using LinqToDB;
using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.Include
{
    static class PropertyQueryBuilder
    {

        internal static IQueryable<TChild> BuildReusableQueryableForProperty<TParent, TChild>(
            IQueryable<TParent> mainQuery,
            PropertyAccessor<TParent, TChild> schema)
            where TParent : class
            where TChild : class
        {
            if (schema.IsMemberEntityTypeIEnumerable)
            {
                throw new ArgumentException($"'{nameof(TChild)}' cannot be an IEnumerable<>.");
            }

            var mainQueryCopy = mainQuery;
            var db = mainQuery.GetDataContext<IDataContext>();

            //join the query passed in to the child table
            var joinExpr = BuildJoinExpression(schema);
            var subSelect = db.GetTable<TChild>().Join(mainQueryCopy, SqlJoinType.Inner, joinExpr, (c, p) => c);

            return subSelect;
        }

        internal static IQueryable<TChild> BuildQueryableForProperty<TParent, TChild>(IQueryable<TParent> mainQuery,
            PropertyAccessor<TParent, TChild> schema)
            where TParent : class
            where TChild : class
        {
            if (schema.IsMemberEntityTypeIEnumerable)
            {
                throw new ArgumentException($"'{nameof(TChild)}' cannot be an IEnumerable<>.");
            }

            IQueryable mainQueryCopy = mainQuery;
            var db = mainQuery.GetDataContext<IDataContext>();


            ParameterExpression parentParam = null;
            ParameterExpression childParam = null;

            //join the child table in the main section of the new to
            //the same table in the EXISTS function            

            var predicate = schema.AssociationDescriptor
                .GetPredicate(schema.DeclaringType, schema.MemberEntityType);

            Expression innerCondition = null;
            Type innerWhereDelegate = null;
            Type outerWhereDelegate = null;
            Type innerWhereType = null;

            if (predicate != null)
            {
                parentParam = Expression.Parameter(schema.MemberEntityType, "parent");
                childParam = Expression.Parameter(schema.MemberEntityType, "child");


                innerWhereType = schema.MemberEntityType;
                innerWhereDelegate = typeof(Func<TChild, bool>);
                innerCondition = BuildJoinClauseByPrimaryKey(schema.ChildEntityDescriptor, parentParam, childParam);
                outerWhereDelegate = typeof(Func<TChild, bool>);
                var joinExpr = BuildJoinExpression(schema);

                var subSelect = db.GetTable<TChild>().Join(mainQuery, SqlJoinType.Inner, joinExpr, (c, p) => c);                
                mainQueryCopy = subSelect;
            }
            else
            {
                parentParam = Expression.Parameter(schema.DeclaringType, "parent");
                childParam = Expression.Parameter(schema.MemberEntityType, "child");

                innerWhereType = schema.DeclaringType;
                innerWhereDelegate = typeof(Func<TParent, bool>);
                innerCondition = BuildJoinClauseByForeignKey(schema, null, parentParam, childParam);
                outerWhereDelegate = typeof(Func<TChild, bool>);
            }

            // build theIQ.Where( pe2 => innerCondition )
            var queryableType = typeof(Queryable);

            var innerWhere = Expression.Call(
                queryableType,
                nameof(Enumerable.Where),
                new Type[] { innerWhereType },
                new Expression[]
                {
                    mainQueryCopy.Expression,
                    Expression.Lambda(innerWhereDelegate, innerCondition,
                            new ParameterExpression[] { parentParam })
                }
            );

            // build x.Where( pe2 => innerWhere ).Any()
            var anyCall = Expression.Call(
                queryableType, nameof(Enumerable.Any), new Type[] { innerWhereType }, innerWhere
            );

            var childTable = db.GetTable<TChild>();

            // build x.Where( pe1 => anyCall )
            var outerWhere = Expression.Call(
                queryableType,
                nameof(Enumerable.Where),
                new Type[] { schema.MemberEntityType },
                new Expression[]
                {
                    childTable.Expression,
                    Expression.Lambda(outerWhereDelegate, anyCall, new ParameterExpression[] { childParam })
                }
            );

            var resultingQuery = Linq.Internals.CreateExpressionQueryInstance<TChild>(db, outerWhere);

            return resultingQuery;

        }



        private static Expression BuildJoinClauseByPrimaryKey(EntityDescriptor childDesc, 
            ParameterExpression childParam1, ParameterExpression childParam2)
        {
            var pkColumns = childDesc.Columns.Where(x => x.IsPrimaryKey);
            if (!pkColumns.Any())
            {
                throw new PrimaryKeyNotFoundException($"No primary key defined for type '{childDesc.ObjectType.Name}'");
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

        private static Expression BuildJoinClauseByForeignKey<TParent, TChild>(
                PropertyAccessor<TParent, TChild> schema,
                LambdaExpression predicate,
                ParameterExpression parentParam,
                ParameterExpression childParam)
            where TParent : class
            where TChild : class
        {
            var assoc = schema.AssociationDescriptor;

            Expression previousExpr = predicate == null ? null : 
                SubstituteParameters(predicate, parentParam, childParam);

            if(previousExpr is LambdaExpression lambdaExpression)
            {
                previousExpr = lambdaExpression.Body;
            }

            for (int i = 0; i < assoc.ThisKey.Length; i++)
            {
                Expression childProperty = Expression.Property(childParam, assoc.OtherKey[i]);
                Expression parentProperty = Expression.Property(parentParam, assoc.ThisKey[i]);

                if (childProperty.Type != parentProperty.Type)
                {
                    parentProperty = Expression.Convert(parentProperty, childProperty.Type);
                }

                var equals = Expression.Equal(parentProperty, childProperty);
                if (previousExpr == null)
                {
                    previousExpr = equals;
                }
                else
                {
                    previousExpr = Expression.AndAlso(previousExpr, equals);
                }
            }

            return previousExpr;
        }

        private static LambdaExpression SubstituteParameters(LambdaExpression predicate, 
            ParameterExpression parentParam, ParameterExpression childParam)
        {
            return Expression.Lambda(predicate.Body, parentParam, childParam);
        }

        private static Expression<Func<TChild, TParent, bool>> BuildJoinExpression<TParent, TChild>(
                PropertyAccessor<TParent, TChild> schema)
            where TParent : class
            where TChild : class
        {
            var predicate = schema.AssociationDescriptor.GetPredicate(schema.DeclaringType, schema.MemberEntityType);

            var parentParam = predicate?.Parameters.FirstOrDefault() ?? Expression.Parameter(schema.DeclaringType, "p");

            var childParam = predicate?.Parameters.Skip(1).FirstOrDefault() ?? 
                                    Expression.Parameter(schema.MemberEntityType, "c");

            var previousExpr = BuildJoinClauseByForeignKey(schema, predicate, parentParam, childParam);

            if (previousExpr is LambdaExpression lambda)
            {
                previousExpr = lambda.Body;
            }

            var expr = Expression.Lambda<Func<TChild, TParent, bool>>(previousExpr, childParam, parentParam);
            return expr;
        }
    }
}
