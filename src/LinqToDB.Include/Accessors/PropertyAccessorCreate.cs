using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Include
{
    static class PropertyAccessor
    {
        internal static PropertyAccessor<TEntity, TProperty> Create<TEntity, TProperty>
            (MemberExpression exp, IPropertyAccessor parentAccessor, IRootAccessor root)
            where TEntity : class
            where TProperty : class
        {
            var pathParts = PathWalker.GetPath(exp);
            var newAccessor = root.GetByPath<TEntity, TProperty>(pathParts) ??
                                        new PropertyAccessor<TEntity, TProperty>(exp, root.MappingSchema);

            if (parentAccessor != null && !newAccessor.Properties.Contains(parentAccessor))
            {
                var genericParentAccessor = (IPropertyAccessor<TProperty>)parentAccessor;
                newAccessor.PropertiesOfTClass.Add(genericParentAccessor);
            }

            return newAccessor;
        }
    }
}