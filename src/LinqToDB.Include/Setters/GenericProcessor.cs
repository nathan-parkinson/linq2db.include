using ExpressionKey;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Include.Setters
{
    class GenericProcessor
    {
        private static readonly Type _objType = typeof(object);
        private static readonly Type _valueType = typeof(ValueType);
        
        protected static readonly ConcurrentDictionary<Type, IGenericProcessor> _processors = 
            new ConcurrentDictionary<Type, IGenericProcessor>();

        protected GenericProcessor()
        {

        }

        internal static List<T> ProcessEntities<T>(EntityPool entityPool, IDataContext db, IEnumerable<T> entities) 
            where T : class
        {
            if(!entityPool.ConsolidateEntities)
            {
                return entities.ToList();
            }

            var type = typeof(T);
            
            var processor = _processors.GetOrAdd(type, t => new GenericProcessor<T>());            
            var runProcessor = processor as IGenericProcessor<T>;
            
            return runProcessor.Process(entityPool, db, entities).ToList();
        }

        protected static Type GetRealBaseType(Type type)
        {
            var baseType = type.BaseType;
            var objType = typeof(object);

            while (baseType != objType && baseType != typeof(ValueType) && !type.IsInterface && !baseType.IsInterface)
            {
                type = baseType;
                baseType = type.BaseType;
            }

            return type;
        }

    }
}
