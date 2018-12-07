using System;
using System.Collections.Generic;

namespace LinqToDB.Utils
{
    interface IPropertyAccessor
    {
        Type DeclaringType { get; }
        Type MemberType { get; }
        Type MemberEntityType { get; }
        HashSet<IPropertyAccessor> Properties { get; }
        string PropertyName { get; }
        bool IsMemberTypeICollection { get; }


        IPropertyAccessor FindAccessor(List<string> pathParts);
    }

    interface IPropertyAccessor<out TClass> : IPropertyAccessor where TClass : class
    {

    }
}