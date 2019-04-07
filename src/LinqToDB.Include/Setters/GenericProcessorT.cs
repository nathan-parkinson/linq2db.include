using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Include.Setters
{
    internal sealed class GenericProcessor<T> : GenericProcessor, IGenericProcessor<T> where T : class
    {
        private static readonly Func<EntityPool, IDataContext, IEnumerable<T>, IEnumerable<T>> _func;

        static GenericProcessor()
        {
            var type = typeof(T);
            var baseType = GetRealBaseType(type);

            //TODO Only build this once
            var instance = Expression.Parameter(typeof(EntityPool), "ep");
            var contextParam = Expression.Parameter(typeof(IDataContext), "db");
            var entitiesParam = Expression.Parameter(typeof(IEnumerable<T>), "entities");

            //use this to call the process entities method
            var methodExpr = Expression.Call(instance, nameof(EntityPool.ProcessEntities),
                new Type[] { type, baseType },
                contextParam,
                entitiesParam);

            var lambda = Expression.Lambda<Func<EntityPool, IDataContext, IEnumerable<T>, IEnumerable<T>>>(methodExpr,
                instance,
                contextParam,
                entitiesParam);

            _func = lambda.Compile();            
        }
        
        IEnumerable<T> IGenericProcessor<T>.Process(EntityPool entityPool, IDataContext db, IEnumerable<T> entities)
        {
            var result = _func(entityPool, db, entities);            
            return result;
        
        }
    }
}
