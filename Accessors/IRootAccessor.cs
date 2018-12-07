using LinqToDB.Mapping;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Utils
{
    interface IRootAccessor
    {
        PropertyAccessor<TEntity, TProperty> GetByPath<TEntity, TProperty>(List<string> pathParts) where TEntity : class where TProperty : class;
        MappingSchema MappingSchema { get; }
    }
}