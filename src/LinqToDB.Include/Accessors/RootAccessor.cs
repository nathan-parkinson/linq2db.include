using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Include
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

            //TODO get by Type and PropertyName to account for multiple inherited classes with same PropertyName
            var accessor = Properties.SingleOrDefault(x => x.PropertyName == thisPath);
            if (accessor == null)
            {
                return null;
            }

            var result = accessor.FindAccessor(pathParts.Skip(1).ToList());

            return result as PropertyAccessor<TEntity, TProperty>;
        }

        public void LoadMap(List<TClass> entities, IQueryable<TClass> query)
        {
            foreach (var propertyAccessor in Properties.OrderBy(x => x.PropertyName))
            {
                if (propertyAccessor is PropertyAccessor<TClass> accessorImpl)
                {
                    accessorImpl.Load(entities, query);
                }
                else if (typeof(TClass).IsAssignableFrom(propertyAccessor.DeclaringType))
                {
                    dynamic dynamicAccessor = propertyAccessor;
                    LoadForInheritedType(dynamicAccessor, entities, query);
                }
                else
                {
                    //TODO Create own exception type and ad a proper message
                    throw new Exception();
                }
            }
        }

        private static void LoadForInheritedType<T>(IPropertyAccessor<T> accessor, List<TClass> propertyEntities, IQueryable<TClass> query)
            where T : class
        {
            var accessorImpl = (PropertyAccessor<T>)accessor;

            var entitiesOfType = propertyEntities.OfType<T>().ToList();
            var queryOfType = query.OfType<T>();

            accessorImpl.Load(entitiesOfType, queryOfType);
        }


        public MappingSchema MappingSchema { get; }
    }
}
