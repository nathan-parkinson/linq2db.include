﻿using LinqToDB.Include.Setters;
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

        IPropertyAccessor IRootAccessor.GetByPath(List<string> pathParts)
        {
            var thisPath = pathParts.First();

            //TODO get by Type and PropertyName to account for multiple inherited classes with same PropertyName
            var accessor = Properties.SingleOrDefault(x => x.PropertyName == thisPath);
            if (accessor == null)
            {
                return null;
            }

            var result = accessor.FindAccessor(pathParts.Skip(1).ToList());

            return result;
        }


        public void LoadMap(List<TClass> entities, IQueryable<TClass> query)
        {
            var entityPool = new EntityPool();
            var deDupe = GenericProcessor.ProcessEntities(entityPool, query.GetDataContext<IDataContext>(), entities);
            entities.Clear();
            entities.AddRange(deDupe);

            foreach (var propertyAccessor in Properties.OrderBy(x => x.PropertyName))
            {
                if (propertyAccessor is PropertyAccessor<TClass> accessorImpl)
                {
                    accessorImpl.Load(entityPool, entities, query);
                }
                else if (typeof(TClass).IsAssignableFrom(propertyAccessor.DeclaringType))
                {
                    dynamic dynamicAccessor = propertyAccessor;
                    LoadForInheritedType(entityPool, dynamicAccessor, entities, query);
                }
                else
                {
                    throw new PropertyAccessorNotFoundException($"PropertyAccessor<{typeof(TClass).Name}> not found");
                }
            }
        }

        private static void LoadForInheritedType<T>(EntityPool entityPool, IPropertyAccessor<T> accessor, List<TClass> propertyEntities, IQueryable<TClass> query)
            where T : class
        {
            var accessorImpl = (PropertyAccessor<T>)accessor;

            var entitiesOfType = propertyEntities.OfType<T>().ToList();
            var queryOfType = query.OfType<T>();

            accessorImpl.Load(entityPool, entitiesOfType, queryOfType);
        }


        public MappingSchema MappingSchema { get; }
    }
}
