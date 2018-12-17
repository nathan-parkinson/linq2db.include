using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Include
{
    class PropertyVisitor<TClass> : ExpressionVisitor
        where TClass : class
    {
        private IPropertyAccessor latestAccessor;
        private readonly IRootAccessor<TClass> _rootAccessor;

        internal PropertyVisitor(IRootAccessor<TClass> rootAccessor)
        {
            _rootAccessor = rootAccessor;
        }


        private static void AddFilterForInheritedType<T, TProperty>(IPropertyAccessor<T> accessor, 
            Expression<Func<TProperty, bool>> includeFilter)
            where T : class
            where TProperty : class
        {
            var accessorImpl = (PropertyAccessor<T, TProperty>)accessor;
            accessorImpl.AddFilter(includeFilter);
        }

        public IRootAccessor<TClass> MapProperties<TProperty>(Expression<Func<TClass, TProperty>> expr, 
            Expression<Func<TProperty, bool>> includeFilter = null)
            where TProperty : class
        {
            Visit(expr);
            var accessor = latestAccessor as IPropertyAccessor<TClass>;
            if (includeFilter != null)
            {
                if (latestAccessor is PropertyAccessor<TClass, TProperty> accessorImpl)
                {
                    accessorImpl.AddFilter(includeFilter);
                }
                else if (typeof(TClass).IsAssignableFrom(latestAccessor.DeclaringType))
                {
                    dynamic dynamicAccessor = latestAccessor;
                    AddFilterForInheritedType(dynamicAccessor, includeFilter);
                }
                else
                {
                    throw new PropertyAccessorNotFoundException($"PropertyAccessor<{typeof(TClass).Name}, {typeof(TProperty).Name}> not found");
                }
            }

            //dupes at this point
            if (!_rootAccessor.Properties.Contains(accessor))
            {
                _rootAccessor.Properties.Add(accessor);
            }

            return _rootAccessor;
        }


        /*
        public static RootAccessor<TClass> RunVisitor<TClass, TProperty>(Expression<Func<TClass, TProperty>> expr, RootAccessor<TClass> root = null)
            where TClass : class
            where TProperty : class
        {
            if (root == null)
            {
                root = new RootAccessor<TClass>();
            }

            var visitor = new PropertyVisitor(root);
            visitor.Visit(expr);
            var accessor = visitor.latestAccessor as IPropertyAccessor<TClass>;
            //dupes at this point
            if (!root.Properties.Contains(accessor))
            {
                root.Properties.Add(accessor);
            }

            return root;
        }
        */

        protected override Expression VisitMember(MemberExpression node)
        {
            //check PropertyAccessor for the property does not already exist
            var localAccessor = CreateAccessor(node);
            latestAccessor = localAccessor;
            return base.VisitMember(node);
        }

        private IPropertyAccessor CreateAccessor(MemberExpression node)
        {
            //if accessor already exists, then return it
            var declaringType = GetTypeToUse(node.Member.DeclaringType);
            var nodeType = GetTypeToUse(node.Type);

            if (latestAccessor != null && latestAccessor.DeclaringType == declaringType && 
                latestAccessor.PropertyName == node.Member.Name)
            {
                return latestAccessor;
            }

            //otherwise make a new one and return that
            var param = Expression.Parameter(typeof(MemberExpression), "exp");
            var param2 = Expression.Parameter(typeof(IPropertyAccessor), "accessor");
            var param3 = Expression.Parameter(typeof(IRootAccessor), "root");

            var methodCallExpression = Expression.Call(typeof(PropertyAccessor), nameof(PropertyAccessor.Create),
                new Type[] { declaringType, nodeType }, param, param2, param3);

            var func = Expression.Lambda<Func<MemberExpression, IPropertyAccessor, IRootAccessor, IPropertyAccessor>>(
                methodCallExpression, param, param2, param3).Compile();

            return func(node, latestAccessor, _rootAccessor);
        }

        internal static Type GetTypeToUse(Type type)
        {
            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();

                if (genericTypeDefinition.GetInterfaces()
                            .Any(t => t.IsGenericType &&
                                      t.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return type;
        }
    }
}
