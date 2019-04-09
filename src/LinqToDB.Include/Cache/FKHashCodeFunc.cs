using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Include.Cache
{
    class FKHashCodeFunc<TThis, TOther> : IFKHashCodeFunc
                where TThis : class
                where TOther : class
    {

        public FKHashCodeFunc(Func<TThis, int> thisHashCodeFunc,
            Func<TOther, int> otherHashCodeFunc,
            Func<TThis, TOther, bool> associationPredicate)
        {
            ThisHashCodeFunc = thisHashCodeFunc;
            OtherHashCodeFunc = otherHashCodeFunc;
            AssociationPredicate = associationPredicate;
        }

        internal Func<TThis, int> ThisHashCodeFunc { get; }
        internal Func<TOther, int> OtherHashCodeFunc { get; }
        internal Func<TThis, TOther, bool> AssociationPredicate { get; }
    }
}
