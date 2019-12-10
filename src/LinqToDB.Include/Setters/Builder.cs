using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Include.Setters
{
    internal class Builder : ExpressionKey.KeyBuilder
    {
        public Builder(MappingSchema schema)
        {
            foreach (var type in schema.GetEntites())
            {
                var desc = schema.GetEntityDescriptor(type);

                var param = Expression.Parameter(type);
                foreach (var key in desc.Columns.Where(x => x.IsPrimaryKey).OrderBy(x => x.PrimaryKeyOrder))
                {
                    CreateExpression(param, type, key.MemberName, key.MemberType);
                }


                foreach (var assoc in desc.Associations)
                {
                    var memberType = assoc.MemberInfo.GetMemberUnderlyingType();
                    if (memberType.IsIEnumerable())
                    {
                        CreateRelationshipExpression(assoc, type, memberType.GetTypeToUse(), memberType);
                    }
                    else
                    {
                        CreateRelationshipExpression(assoc, type, memberType);
                    }
                }
            }
        }

        private void CreateRelationshipExpression(AssociationDescriptor assoc, Type type, Type memberType)
        {
            MethodInfo method = typeof(Builder).GetMethod(nameof(Builder.CreateRelationshipExpression));
            MethodInfo generic = method.MakeGenericMethod(type, memberType);
            generic.Invoke(this, new object[] { assoc });
        }


        private void CreateRelationshipExpression(AssociationDescriptor assoc, Type type, Type memberType, Type propertyType)
        {
            MethodInfo method = typeof(Builder).GetMethod(nameof(Builder.CreateRelationshipEnumerableExpression));
            MethodInfo generic = method.MakeGenericMethod(type, memberType, propertyType);
            generic.Invoke(this, new object[] { assoc });
        }

        private void CreateExpression(ParameterExpression param, Type type, string member, Type memberType)
        {
            MethodInfo method = typeof(Builder).GetMethod(nameof(Builder.CreateAndAddKey));
            MethodInfo generic = method.MakeGenericMethod(type, memberType);
            generic.Invoke(this, new object[] { param, member });
        }

        public void CreateAndAddKey<T, U>(ParameterExpression param, string member)
        {
            var key = CreateExpression<T, U>(param, member);
            AddKey<T, U>(key);
        }

        public Expression<Func<T, U>> CreateExpression<T, U>(ParameterExpression param, string member)
        {
            MemberExpression pk = Expression.PropertyOrField(param, member);
            Expression<Func<T, U>> lambda = Expression.Lambda<Func<T, U>>(pk, param);            
            return lambda;
        }

        public void CreateRelationshipExpression<T, U>(AssociationDescriptor assoc)
        {
            var t = BuildJoinExpression<T, U>(assoc, typeof(T), typeof(U)) as Expression<Func<T, U, bool>>;
            var property = CreateExpression<T, U>(t.Parameters[0], assoc.MemberInfo.Name);
            AddRelationship(property, t);
        }

        public void CreateRelationshipEnumerableExpression<T, U, P>(AssociationDescriptor assoc)
            where P : IEnumerable<U>
        {
            var t = BuildJoinExpression<T, U>(assoc, typeof(T), typeof(U)) as Expression<Func<T, U, bool>>;
            var property = CreateExpression<T, P>(t.Parameters[0], assoc.MemberInfo.Name);
            AddRelationship<T, P, U>(property, t);
        }

        internal static Expression<Func<TParent, TChild, bool>> BuildJoinExpression<TParent, TChild>(
        AssociationDescriptor assoc, Type declaringType, Type memberEntityType)
        {
            var predicate = assoc.GetPredicate(declaringType, memberEntityType);

            var parentParam = predicate?.Parameters.FirstOrDefault() ?? Expression.Parameter(declaringType, "p");

            var childParam = predicate?.Parameters.Skip(1).FirstOrDefault() ??
                                    Expression.Parameter(memberEntityType, "c");

            var previousExpr = BuildJoinClauseByForeignKey(assoc, predicate, parentParam, childParam);

            if (previousExpr is LambdaExpression lambda)
            {
                previousExpr = lambda.Body;
            }

            var expr = Expression.Lambda<Func<TParent, TChild, bool>>(previousExpr, parentParam, childParam);
            return expr;
        }


        private static Expression BuildJoinClauseByForeignKey(
                AssociationDescriptor assoc,
                LambdaExpression predicate,
                ParameterExpression parentParam,
                ParameterExpression childParam)
        {
            
            Expression previousExpr = predicate == null ? null :
                SubstituteParameters(predicate, parentParam, childParam);

            if (previousExpr is LambdaExpression lambdaExpression)
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
    }
}
