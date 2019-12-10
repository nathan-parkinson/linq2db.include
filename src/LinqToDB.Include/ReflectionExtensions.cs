using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.Include
{
    static class ReflectionExtensions
    { 
        internal static Type GetTypeToUse(this Type type)
        {
            if (type.IsGenericType)
            {
                if (type.IsInterface && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return type.GetGenericArguments()[0];
                }

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
                    var def = type.GetGenericTypeDefinition();

                    return def == typeof(IEnumerable<>) || type.GetGenericTypeDefinition().GetInterfaces()
                                .Any(x => x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                }

                var genericTypeDefinition = type.GetGenericTypeDefinition();

                return genericTypeDefinition.GetInterfaces()
                            .Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            }

            return false;
        }

        internal static Type GetMemberUnderlyingType(this MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                case MemberTypes.Event:
                    return ((EventInfo)member).EventHandlerType;
                default:
                    throw new ArgumentException("MemberInfo must be if type FieldInfo, PropertyInfo or EventInfo", nameof(member));
            }
        }
    }
}
