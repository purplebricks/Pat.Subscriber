using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
{
    public class AssemblyScanner
    {
        private readonly HashSet<Type> _types;

        public AssemblyScanner()
        {
            _types = new HashSet<Type>();
        }
        private static bool IsHandlerInterface(Type type)
            => type.IsGenericType
               && type.GetGenericTypeDefinition() == typeof(IHandleEvent<>);

        private static bool IsHandler(Type type)
            => type.GetInterfaces().Any(IsHandlerInterface);

        public AssemblyScanner ScanAssemblyContainingType<T>()
        {
            foreach (var type in Assembly.GetAssembly(typeof(T)).GetTypes().Where(IsHandler))
            {
                _types.Add(type);
            }

            return this;
        }

        public void RegisterHandlers(IServiceCollection serviceCollection)
        {
            foreach (var handler in _types)
            {
                serviceCollection.AddTransient(handler);
            }
        }
    }
}