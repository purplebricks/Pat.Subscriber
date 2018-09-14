using Microsoft.Extensions.Logging;
using StructureMap.Pipeline;

namespace Pat.Subscriber.StructureMap4DependencyResolution.Logging
{
    public class LoggerInstance<T> : LambdaInstance<ILogger<T>>
    {
        public LoggerInstance() : base(LoggerBuilder.Build<T>())
        {
        }
    }
}