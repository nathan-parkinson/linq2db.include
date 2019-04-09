using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Include.Cache
{
    class PropertySetterCache<TElement, TValue> : IPropertySetterCache
    {
        public PropertySetterCache(Action<TElement, TValue> setter)
        {
            Setter = setter;
        }

        public Action<TElement, TValue> Setter { get; }
    }
}
