using System;
using System.Collections.Generic;
using System.Reflection;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Extension class for the <see cref="System.Type"/> class
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Returns true of <paramref name="t"/> is a generic collection type
        /// </summary>
        /// <param name="t">The <see cref="System.Type"/> to be checked</param>
        /// <returns>True if the type is a generic collection, false otherwise</returns>
        public static bool IsGenericCollection(this Type t)
        {
#if NETCORE
            return (t.GetTypeInfo().IsGenericType &&
                    typeof(IEnumerable<>).IsAssignableFrom(t.GetGenericTypeDefinition()));
#else
            return (t.IsGenericType && typeof (IEnumerable<>).IsAssignableFrom(t.GetGenericTypeDefinition()));
#endif
        }

        /// <summary>
        /// Returns true if <paramref name="t"/> is a generic nullable
        /// </summary>
        /// <param name="t">The <see cref="System.Type"/> to be checked</param>
        /// <returns>True if the type is a generic nullable, false otherwise</returns>
        public static bool IsNullable(this Type t)
        {
#if NETCORE
            return t.GetTypeInfo().IsGenericType && typeof(Nullable<>).IsAssignableFrom(t.GetGenericTypeDefinition());
#else
            return t.IsGenericType && typeof (Nullable<>).IsAssignableFrom(t.GetGenericTypeDefinition());
#endif
        }

        ///<summary>
        ///</summary>
        ///<param name="t"></param>
        ///<returns></returns>
        ///<exception cref="ArgumentException"></exception>
        public static object GetDefaultValue(this Type t)
        {
#if NETCORE
            if (t == null || !t.GetTypeInfo().IsValueType || t == typeof(void)) return null;
            if (t.IsNullable()) return null;
            if (t.GetTypeInfo().IsValueType || !t.GetTypeInfo().IsPublic)
            {
                try
                {
                    return Activator.CreateInstance(t);
                }
                catch (Exception e)
                {
                    throw new ArgumentException("Could not create a default instance of the type " + t.FullName, e);
                }
            }
            throw new ArgumentException("Could not determine default value of the type " + t.FullName);
#else
            if (t == null || !t.IsValueType || t == typeof(void)) return null;
            if (t.IsNullable()) return null;
            if (t.IsValueType || !t.IsPublic)
            {
                try
                {
                    return Activator.CreateInstance(t);
                }
                catch (Exception e)
                {
                    throw new ArgumentException("Could not create a default instance of the type " + t.FullName, e);
                }
            }
            throw new ArgumentException("Could not determine default value of the type " + t.FullName);
#endif
        }

    }
}
