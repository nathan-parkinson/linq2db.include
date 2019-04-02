using System.Collections.Generic;

namespace LinqToDB.Include.Setters
{
    interface IGenericProcessor<T> : IGenericProcessor where T : class
    {
        IEnumerable<T> Process(EntityPool entityPool, IDataContext db, IEnumerable<T> entities);
    }
}
