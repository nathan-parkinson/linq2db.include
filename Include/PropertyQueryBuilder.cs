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

            var mainQueryCopy = mainQuery;
            var db = mainQuery.GetDataContext<IDataContext>();

            var childTable = db.GetTable<TChild>();

            var parentParam = Expression.Parameter(schema.DeclaringType, "parent");
            var childParam = Expression.Parameter(schema.MemberEntityType, "child");

            //join the child table in the main section of the new to
            //the same table in the EXISTS function
            var innerCondition = BuildOuterWhereExpression(schema, parentParam, childParam);

            // build theIQ.Where( pe2 => innerCondition )
            var queryableType = typeof(Queryable);

            var innerWhere = Expression.Call(
                queryableType,
                nameof(Enumerable.Where),
                new Type[] { schema.DeclaringType },
                new Expression[]
                {
                    mainQueryCopy.Expression,
                    Expression.Lambda(typeof(Func<TParent, bool>), innerCondition,
                            new ParameterExpression[] { parentParam })
                }
            );

            // build x.Where( pe2 => innerWhere ).Any()
            var anyCall = Expression.Call(
                queryableType, nameof(Enumerable.Any), new Type[] { schema.DeclaringType }, innerWhere
            );
            
            // build x.Where( pe1 => anyCall )
            var outerWhere = Expression.Call(
                queryableType,
                nameof(Enumerable.Where),
                new Type[] { schema.MemberEntityType },
                new Expression[]
                {
                    childTable.Expression,
                    Expression.Lambda(typeof(Func<TChild, bool>), anyCall, new ParameterExpression[] { childParam })
                }
            );

            var resultingQuery = Linq.Internals.CreateExpressionQueryInstance<TChild>(db, outerWhere);

            return resultingQuery;
        }

        private static Expression BuildOuterWhereExpression<TParent, TChild>(PropertyAccessor<TParent, TChild> schema, ParameterExpression parentParam, ParameterExpression childParam)
            where TParent : class
            where TChild : class
        {
            var assoc = schema.AssociationDescriptor;
            BinaryExpression previousExpr = null;
            for (int i = 0; i < assoc.ThisKey.Length; i++)
            {
                Expression childProperty = Expression.Property(childParam, assoc.OtherKey[i]);
                Expression parentProperty = Expression.Property(parentParam, assoc.ThisKey[i]);

                if (childProperty.Type != parentProperty.Type)
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

            return previousExpr;
        }

        private static Expression<Func<TChild, TParent, bool>> BuildJoinExpression<TParent, TChild>(PropertyAccessor<TParent, TChild> schema)
            where TParent : class
            where TChild : class
        {
            var childParam = Expression.Parameter(schema.MemberEntityType, "c");
            var parentParam = Expression.Parameter(schema.DeclaringType, "p");
            var previousExpr = BuildOuterWhereExpression(schema, parentParam, childParam);
            var expr = Expression.Lambda<Func<TChild, TParent, bool>>(previousExpr, childParam, parentParam);
            return expr;
        }
    }

}
