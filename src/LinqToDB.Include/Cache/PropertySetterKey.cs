using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Include.Cache
{

    internal class PropertySetterKey : IEquatable<PropertySetterKey>
    {

        internal PropertySetterKey(Type elementType, Type valueType, string propertyName)
        {
            ElementType = elementType;
            ValueType = valueType;
            PropertyName = propertyName;
        }

        public Type ElementType { get; }
        public Type ValueType { get; }
        public string PropertyName { get; }

        public override bool Equals(object obj) => obj is PropertySetterKey key && Equals(key);

        public bool Equals(PropertySetterKey key) 
            => EqualityComparer<Type>.Default.Equals(ElementType, key.ElementType) &&
                   EqualityComparer<Type>.Default.Equals(ValueType, key.ValueType) &&
                   PropertyName == key.PropertyName;

        public override int GetHashCode()
        {
            var hashCode = -593368060;
            hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(ElementType);
            hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(ValueType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PropertyName);
            return hashCode;
        }        
    }

}
