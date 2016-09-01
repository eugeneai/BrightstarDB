using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;

namespace BrightstarDB.Compatibility
{
    public static class CompatibilityExtensions
    {
        public static void Close(this Stream stream) { stream.Dispose(); }
        public static void Close(this StringWriter writer) { writer.Dispose();}
        public static void Close(this StreamWriter writer) { writer.Dispose();}
        public static void Close(this BinaryWriter writer) { writer.Dispose();}
        public static void Close(this XmlWriter writer) { writer.Dispose();}

        public static bool IsEnum(this Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }

        public static bool IsSubclassOf(this Type type, Type other)
        {
            return type.GetTypeInfo().IsSubclassOf(other);
        }
    }
}
