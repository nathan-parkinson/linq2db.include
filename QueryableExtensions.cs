using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.Utils
{
    public static class QueryableExtensions
    {
        public static T GetDataContext<T>(this IQueryable query) where T : IDataContext
        {
            var expressionQuery = query as Linq.IExpressionQuery;            
            if (!(expressionQuery?.DataContext is T))
            {
                throw new InvalidCastException($"DataContext '{typeof(T).Name}' not found");
            }

            return (T)expressionQuery.DataContext;
        }
    }
}
