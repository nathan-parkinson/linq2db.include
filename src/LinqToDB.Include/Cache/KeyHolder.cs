using System;
using System.Linq.Expressions;

namespace LinqToDB.Include.Cache
{
    class KeyHolder : IEquatable<KeyHolder>
    {
        public string Key { get; set; }
        public KeyType Type { get; set; }
        public ConstantExpression Constant { get; set; }

        public bool Equals(KeyHolder other)
        {
            return other != null && Key == other.Key && Type == other.Type && Constant?.Value == other.Constant?.Value;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + Type.GetHashCode();
                if (Key != null)
                {
                    hash = hash * 23 + Key.GetHashCode();
                }

                if (Constant != null)
                {
                    hash = hash * 23 + Constant.Value.GetHashCode();
                }

                return hash;
            }
        }
    }

    enum KeyType
    {
        Property = 0,
        Constant = 1
    }

}