using System;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using StructureMap;

namespace Pat.Subscriber.StructureMap4DependencyResolution.Logging
{
    public static class LoggerBuilder
    {
        public static Expression<Func<IContext, ILogger<T>>> Build<T>()
        {
            Expression<Func<IContext, ILogger<T>>> expression = (context) =>
                context.GetInstance<ILoggerFactory>().CreateLogger<T>();

            return expression;
        }
    }
}