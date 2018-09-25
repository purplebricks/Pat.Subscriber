using System;
using Microsoft.Extensions.Logging;
using StructureMap.Building;
using StructureMap.Pipeline;

namespace Pat.Subscriber.StructureMap4DependencyResolution.Logging
{
    public class LoggerInstanceFactory : Instance
    {
        public override IDependencySource ToDependencySource(Type pluginType)
        {
            throw new NotSupportedException();
        }

        public override Instance CloseType(Type[] types)
        {
            var instanceType = typeof(LoggerInstance<>).MakeGenericType(types);
            return Activator.CreateInstance(instanceType) as Instance;
        }

        public override string Description => "Build ILogger<T>";

        public override Type ReturnedType => typeof(ILogger<>);
    }
}