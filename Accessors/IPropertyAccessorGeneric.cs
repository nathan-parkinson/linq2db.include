using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.Utils
{
    interface IPropertyAccessor<out TClass> : IPropertyAccessor where TClass : class
    {

    }
}
