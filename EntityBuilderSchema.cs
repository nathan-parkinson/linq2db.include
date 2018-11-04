using LinqToDB.Mapping;
using System;

namespace LinqToDB.Utils
{
    class EntityBuilderSchema
    {
        internal EntityBuilderSchema()
        {

        }

        internal EntityBuilderSchema(string propertyName, Type entityType, Type propertyType, Type parentType, EntityDescriptor parentDesc, AssociationDescriptor assoc)
        {
            PropertyName = propertyName;
            ChildEntityType = entityType;
            PropertyType = propertyType;
            ParentType = parentType;

            IsPropertyICollection = propertyType.IsICollection();
            IsPropertyIEnumerable = propertyType.IsIEnumerable();
            IsChildEntityIEnumerable = entityType.IsIEnumerable();

            ParentEntityDescriptor = parentDesc;
            ParentToChildAssociationDescriptor = assoc;
        }

        internal Type ParentType { get; }
        internal Type PropertyType { get; }
        internal Type ChildEntityType { get; }
        internal string PropertyName { get; }

        internal bool IsPropertyIEnumerable { get; }
        internal bool IsPropertyICollection { get; }
        internal bool IsChildEntityIEnumerable { get; }

        internal EntityDescriptor ParentEntityDescriptor { get; }
        internal AssociationDescriptor ParentToChildAssociationDescriptor { get; }
    }
}
