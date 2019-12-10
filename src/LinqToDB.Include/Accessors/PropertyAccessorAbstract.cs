using ExpressionKey;
using LinqToDB.Include.Setters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.Include
{
    abstract class PropertyAccessor<TClass> : IPropertyAccessor<TClass> where TClass : class
    {
        protected string _propertyName;
        protected Type _declaringType;
        protected Type _memberType;
        protected Type _memberEntityType;
        protected Type _memberEntityBaseType;


        protected bool _isMemberTypeIEnumerable;
        protected bool _isMemberEntityTypeIEnumerable;

        internal abstract void Load(IEntityPool entityPool, List<TClass> entities, IQueryable<TClass> query);

        public string PropertyName { get => _propertyName; }
        public Type DeclaringType { get => _declaringType; }
        public Type MemberType { get => _memberType; }
        public Type MemberEntityType { get => _memberEntityType; }
        public Type MemberEntityBaseType { get => _memberEntityBaseType; }
        
        public bool IsMemberTypeIEnumerable { get => _isMemberTypeIEnumerable; }
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
