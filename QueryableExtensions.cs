using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.Utils
{
    public static class QueryableExtensions
    {
        public static T GetDataContext<T, U>(this IQueryable<U> query) where T : IDataContext
        {
            var propertyInfo = query.GetType().GetProperty("DataContext");
            var context = (IDataContext)propertyInfo.GetValue(query);

            if (!(context is T))
            {
                throw new InvalidCastException($"DataContext '{typeof(T).Name}' not found");
            }

            return (T)context;
        }
    }
}
