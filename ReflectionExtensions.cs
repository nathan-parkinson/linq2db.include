using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                if (type.IsInterface)
                {
                    return type.GetGenericTypeDefinition().GetInterfaces().Any(x => x.GetGenericTypeDefinition() == typeof(IEnumerable<>));                    
                }

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
                if (type.IsInterface)
                {
                    var def = type.GetGenericTypeDefinition();

                    return def == typeof(ICollection<>) ||
                        def.GetInterfaces().Any(x => x.GetGenericTypeDefinition() == typeof(ICollection<>));
                }

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

        [Obsolete]
        internal static Action<TElement, ICollection<TValue>> CreateICollectionPropertySetter<TElement, TValue>(this Type elementType, string propertyName)
        {
            var pi = elementType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            var mi = pi.GetSetMethod();

            var oParam = Expression.Parameter(elementType, "obj");
            var vParam = Expression.Parameter(typeof(ICollection<TValue>), "val");
            var mce = Expression.Call(oParam, mi, vParam);

            var clearMethod = Expression.Call(vParam, typeof(ICollection<TValue>).GetMethod("Clear"));
            var ifCriteria = Expression.Equal(Expression.Call(oParam, pi.GetGetMethod()), Expression.Constant(null));
            var ifnullSetter = Expression.IfThenElse(ifCriteria, mce, clearMethod);

            var action = Expression.Lambda<Action<TElement, ICollection<TValue>>>(ifnullSetter, oParam, vParam);

            return action.Compile();
        }

        

        internal static Action<TElement, TValue> CreateCollectionPropertySetter<TElement, TValue>(this Type elementType, string propertyName, Type propertyType)
        {
            var pi = elementType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);            
            var mi = pi.GetSetMethod();

            var oParam = Expression.Parameter(elementType, "obj");
            var vParam = Expression.Parameter(typeof(TValue), "val");
            var mce = Expression.Call(Expression.Property(oParam, propertyName), typeof(ICollection<TValue>).GetMethod("Add"), vParam);
            var action = Expression.Lambda<Action<TElement, TValue>>(mce, oParam, vParam);

            return action.Compile();
        }



        internal static Action<TParent> CreatePropertySetup<TParent, TChild>(this Type itemType, string propertyName) where TParent : class where TChild : class
        {   
            var pi = itemType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            var tChildType = pi.PropertyType;
            var parentParam = Expression.Parameter(itemType, "p");
            var property = Expression.Property(parentParam, propertyName);

            var isParamNull = Expression.Equal(property, Expression.Constant(null));

            var collectionTypeToCreate = GetTypeToCreate(tChildType);
            if (collectionTypeToCreate.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new ArgumentException("Collection must have a parameterless constructor. Try instantiating the item in the owners ctor");
            }

            Expression newCollection = null;
            if (collectionTypeToCreate.IsGenericTypeDefinition)
            {
                newCollection = Expression.New(collectionTypeToCreate.MakeGenericType(new Type[] { typeof(TChild) }));
            }
            else
            {
                newCollection = Expression.New(collectionTypeToCreate);
            }

            var mi = pi.GetSetMethod();
            var mce = Expression.Call(parentParam, mi, newCollection);
            
            var @if = Expression.IfThenElse(isParamNull,
                mce,
                Expression.Call(property, typeof(ICollection<TChild>).GetMethod(nameof(ICollection<int>.Clear))));

            var finalCode = Expression.Lambda<Action<TParent>>(@if, parentParam);

            return finalCode.Compile();
        }

        private static Type GetTypeToCreate(Type type)
        {
            if (type.IsClass && !type.IsAbstract)
            {
                return type;
            }

            int typeNum = 3;
            var def = type.GetGenericTypeDefinition();

            if (type.IsInterface)
            {

                if (def == typeof(ISet<>))
                {
                    typeNum = 1;
                }
                else if (def == typeof(IList<>))
                {
                    typeNum = 2;
                }
                else if (def == typeof(ICollection<>))
                {
                    typeNum = 3;
                }
                else
                {
                    typeNum = def.GetInterfaces()
                                .Where(x => x.IsGenericType)
                                .Min(x => x.GetGenericTypeDefinition() == typeof(ISet<>) ? 1 : x.GetGenericTypeDefinition() == typeof(IList<>) ? 2 : 3);
                }
            }
            else
            {
                typeNum = def.GetInterfaces()
                           .Where(t => t.IsGenericType)
                           .Min(x => x.GetGenericTypeDefinition() == typeof(ISet<>) ? 1 : x.GetGenericTypeDefinition() == typeof(IList<>) ? 2 : 3);
            }

            switch (typeNum)
            {
                case 1:
                    return typeof(HashSet<>);
                case 2:
                    return typeof(List<>);
                default:
                    return typeof(Collection<>);
            }
        }
    }
}
