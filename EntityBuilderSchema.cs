using LinqToDB.Mapping;
using System;

namespace LinqToDB.Utils
{
    class EntityBuilderSchema
    {
        internal Type ParentType { get; set; }
        internal Type PropertyType { get; set; }
        internal Type ChildEntityType { get; set; }
        internal string PropertyName { get; set; }
        
        internal bool IsPropertyIEnumerable { get; set; }
        internal bool IsPropertyICollection { get; set; }
        internal bool IsChildEntityIEnumerable { get; set; }
        
        internal EntityDescriptor ParentEntityDescriptor { get; set; }
        internal AssociationDescriptor ParentToChildAssociationDescriptor { get; set; }
    }
}
