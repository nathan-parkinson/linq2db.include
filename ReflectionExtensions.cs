using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.Utils
{
    static class ReflectionExtensions
    {
        internal static Type GetTypeToUse(this Type type)
        {
            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();

                if (genericTypeDefinition.GetInterfaces()
                            .Any(t => t.IsGenericType &&
                                      t.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return type;
        }

        internal static bool IsIEnumerable(this Type type)
        {
            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();

                return genericTypeDefinition.GetInterfaces()
                            .Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            }

            return false;
        }

        internal static bool IsICollection(this Type type)
        {
            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();

                return genericTypeDefinition.GetInterfaces()
                            .Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>));
            }

            return false;
        }

        internal static Action<TElement, TValue> CreatePropertySetter<TElement, TValue>(this Type elementType, string propertyName)
        {
            var pi = elementType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            var mi = pi.GetSetMethod();

            var oParam = Expression.Parameter(elementType, "obj");
            var vParam = Expression.Parameter(typeof(TValue), "val");
            var mce = Expression.Call(oParam, mi, vParam);
            var action = Expression.Lambda<Action<TElement, TValue>>(mce, oParam, vParam);

            return action.Compile();
        }


        internal static Action<TElement, TValue> CreateCollectionPropertySetter<TElement, TValue>(this Type elementType, string propertyName, Type propertyType)
        {
            var pi = elementType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);


            var mi = pi.GetSetMethod();

            var oParam = Expression.Parameter(elementType, "obj");
            var vParam = Expression.Parameter(typeof(TValue), "val");
            var mce = Expression.Call(Expression.Property(oParam, propertyName), propertyType.GetMethod("Add"), vParam);
            //var mce = Expression.Call(oParam, mi, vParam);
            var action = Expression.Lambda<Action<TElement, TValue>>(mce, oParam, vParam);

            return action.Compile();
        }

        static LambdaExpression CreateLambda(Type type)
        {
            var source = Expression.Parameter(
                typeof(IEnumerable<>).MakeGenericType(type), "source");

            var call = Expression.Call(
                typeof(Enumerable), "Last", new Type[] { type }, source);

            return Expression.Lambda(call, source);
        }
    }
}
