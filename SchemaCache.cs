using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LinqToDB.Utils
{
    //TODO Reimplement this caching for the nea IPropertyAccessor types
    static class SchemaCache
    {
        static class Cache<T> where T : class, IDataContext
        {
            internal static readonly ConcurrentDictionary<MemberInfo, IPropertyAccessor> DictionaryCache = new ConcurrentDictionary<MemberInfo, IPropertyAccessor>();
            internal static int MappingSchemaHashCode;
        }

        internal static IPropertyAccessor Get<T>(this T context, MemberInfo member) where T : class, IDataContext
        {
            IPropertyAccessor schema = null;
            if(Cache<T>.MappingSchemaHashCode != context.MappingSchema.GetHashCode())
            {
                Cache<T>.DictionaryCache.Clear();
                return null;
            }


            if(Cache<T>.DictionaryCache.TryGetValue(member, out schema))
            {
                return schema;
            }

            return null;
        }

        internal static bool Set<T>(this T context, PropertyInfo property, IPropertyAccessor schema) 
            where T : class, IDataContext 
                => Cache<T>.DictionaryCache.TryAdd(property, schema);

    }
}
