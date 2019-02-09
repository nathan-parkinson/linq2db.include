using System;

namespace LinqToDB.Include
{
    public interface ICustomPropertyAccessor
    {
        int Key { get; }
    }

    public interface ICustomTypeAccessor
    {
        Type Key { get; }
    }
}
