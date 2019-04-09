using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Include.Cache
{
    internal class FKHashFuncKey : IEquatable<FKHashFuncKey>
    {
        public Type ThisType { get; set; }
        public Type OtherType { get; set; }
        public IEnumerable<KeyHolder> ThisKeys { get; set; }
        public IEnumerable<KeyHolder> OtherKeys { get; set; }

        public override bool Equals(object obj) => obj is FKHashFuncKey key && Equals(key);

        public bool Equals(FKHashFuncKey key) => key != null &&
                   EqualityComparer<Type>.Default.Equals(ThisType, key.ThisType) &&
                   EqualityComparer<Type>.Default.Equals(OtherType, key.OtherType) &&
                   ThisKeys.SequenceEqual(key.ThisKeys) &&
                   OtherKeys.SequenceEqual(key.OtherKeys);

        public override int GetHashCode()
        {
            var hashCode = 1261144875;
            hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(ThisType);
            hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(OtherType);
            foreach (var key in ThisKeys)
            {
                hashCode = hashCode * -1521134295 + key.GetHashCode();
            }

            foreach (var key in OtherKeys)
            {
                hashCode = hashCode * -1521134295 + key.GetHashCode();
            }

            return hashCode;
        }
    }

}
