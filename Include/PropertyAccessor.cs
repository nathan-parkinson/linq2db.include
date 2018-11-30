﻿using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Utils
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
                var genericParentAccessor = (PropertyAccessor<TProperty>)parentAccessor;
                newAccessor.PropertiesOfTClass.Add(genericParentAccessor);
            }

            return newAccessor;
        }
    }

    abstract class PropertyAccessor<TClass> : IPropertyAccessor<TClass> where TClass : class
    {
        protected string _propertyName;
        protected Type _declaringType;
        protected Type _memberType;
        protected Type _memberEntityType;
        protected bool _isMemberTypeICollection;
        protected bool _isMemberEntityTypeIEnumerable;

        internal abstract void Load(List<TClass> entities, IQueryable<TClass> query);

        public string PropertyName { get => _propertyName; }
        public Type DeclaringType { get => _declaringType; }
        public Type MemberType { get => _memberType; }
        public Type MemberEntityType { get => _memberEntityType; }

        public bool IsMemberTypeICollection { get => _isMemberTypeICollection; }
        public bool IsMemberEntityTypeIEnumerable { get => _isMemberEntityTypeIEnumerable; }

        public abstract HashSet<IPropertyAccessor> Properties { get; }

        IPropertyAccessor IPropertyAccessor.FindAccessor(List<string> pathParts)
        {
            var thisPart = pathParts.FirstOrDefault();
            if (thisPart == null)
            {
                return this;
            }

            var property = Properties.SingleOrDefault(x => x.PropertyName == thisPart);
            return property?.FindAccessor(pathParts.Skip(1).ToList());
        }
    }

    class PropertyAccessor<TClass, TProperty> : PropertyAccessor<TClass> where TClass : class where TProperty : class
    {
        public PropertyAccessor(MemberExpression exp, MappingSchema mappingSchema)
        {
            _propertyName = exp.Member.Name;
            _declaringType = exp.Member.DeclaringType;
            _memberType = exp.Type;
            _memberEntityType = typeof(TProperty);
            _isMemberTypeICollection = _memberType.IsICollection();
            _isMemberEntityTypeIEnumerable = _memberEntityType.IsIEnumerable(); 


            var parentDesc = mappingSchema.GetEntityDescriptor(DeclaringType);
            AssociationDescriptor = parentDesc.Associations.Single(x => x.MemberInfo.Name == PropertyName);
        }
        
        public override HashSet<IPropertyAccessor> Properties
        {
            get => new HashSet<IPropertyAccessor>(PropertiesOfTClass);
        }

        //TODO ?decide whether or not to remove this property
        public HashSet<IPropertyAccessor<TProperty>> PropertiesOfTClass
        {
            get;
        } = new HashSet<IPropertyAccessor<TProperty>>();


        
        internal override void Load(List<TClass> entities, IQueryable<TClass> query)
        {
            //TODO need to check if this is inherited member
            //perhaps sort by property name and checkl if is inherited and if property is already loaded


            //get query
            //TODO Change this to get a simpler query for execution and create another method to create a 
            //reusable query for nested properties
            var propertyQuery = GetQuery(query);

            //run query into list
            var propertyEntities = propertyQuery.ToList();
            

            //run nested properties
            foreach (var propertyAccessor in PropertiesOfTClass)
            {
                var accessorImpl = (PropertyAccessor<TProperty>)propertyAccessor;
                accessorImpl.Load(propertyEntities, propertyQuery);
            }

            //set values to entities           
            //TODO change this to a cached Func
            this.SetField(entities, propertyEntities);
        }
        
        internal AssociationDescriptor AssociationDescriptor { get; }
        
        IQueryable<TProperty> GetQuery(IQueryable<TClass> query)
        {
            //TODO change this to create a func to convert any query
            //and then cache the func
            var queryable = PropertyQueryBuilder.BuildQueryableForProperty(query, this);
            return queryable;
        }

    }
    
}