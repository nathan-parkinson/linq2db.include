using LinqToDB.Mapping;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Utils
{
    class RootAccessor<TClass> : IRootAccessor<TClass> where TClass : class
    {        
        public RootAccessor(MappingSchema mappingSchema)
        {
            MappingSchema = mappingSchema;
        }

        //TODO Dictionary or HashSet
        public HashSet<IPropertyAccessor<TClass>> Properties { get; } = new HashSet<IPropertyAccessor<TClass>>();

        PropertyAccessor<TEntity, TProperty> IRootAccessor.GetByPath<TEntity, TProperty>(List<string> pathParts)           
        {
            var thisPath = pathParts.First();

            //TODO get by Type and PropertyName to account for multiple inherited calsses with same PropertyName
            var accessor = Properties.SingleOrDefault(x => x.PropertyName == thisPath);
            if(accessor == null)
            {
                return null;
            }

            var result = accessor.FindAccessor(pathParts.Skip(1).ToList());

            return result as PropertyAccessor<TEntity, TProperty>;
        }

        public void LoadMap(List<TClass> entities, IQueryable<TClass> query)
        {
            foreach (var propertyAccessor in Properties)
            {
                var propertyImpl = (PropertyAccessor<TClass>)propertyAccessor;
                propertyImpl.Load(entities, query);
            }
        }

        public MappingSchema MappingSchema { get; }
    }
}
