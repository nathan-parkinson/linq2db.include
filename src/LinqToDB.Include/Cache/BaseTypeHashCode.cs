using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Include.Cache
{
    class BaseTypeHashCode<TBase> : IBaseTypeHashCode
    {
        public BaseTypeHashCode(Func<TBase, int> hashCodeFunc)
        {
            HashCodeFunc = hashCodeFunc;
        }

        public Func<TBase, int> HashCodeFunc { get; }
    }
}
