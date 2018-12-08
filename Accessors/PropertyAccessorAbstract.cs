﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.Utils
{
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
}