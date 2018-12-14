using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.Include
{
    interface IPropertyAccessor<out TClass> : IPropertyAccessor where TClass : class
    {

    }
}
