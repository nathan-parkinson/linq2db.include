using LinqToDB.Mapping;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Utils
{
    interface IRootAccessor<TClass> : IRootAccessor where TClass : class
    {
        HashSet<IPropertyAccessor<TClass>> Properties { get; }
        void LoadMap(List<TClass> entities, IQueryable<TClass> query);
    }


    interface IRootAccessor
    {
        PropertyAccessor<TEntity, TProperty> GetByPath<TEntity, TProperty>(List<string> pathParts) where TEntity : class where TProperty : class;      
        MappingSchema MappingSchema { get; }
    }
}