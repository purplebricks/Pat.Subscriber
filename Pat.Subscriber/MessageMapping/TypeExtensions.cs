using System;
using System.Reflection;

namespace Pat.Subscriber.MessageMapping
{
    public static class TypeExtensions
    {
        public static string SimpleQualifiedName(this Type t)
        {
            return string.Concat(t.FullName, ", ", t.GetTypeInfo().Assembly.GetName().Name);
        }
    }
}