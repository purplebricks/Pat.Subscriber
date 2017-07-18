using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.PatLite.IoC;
using PB.ITOps.Messaging.PatLite.Policy;

namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
{
    public static class PatLiteDependencyHelper
    {
        private static bool IsHandlerInterface(Type type)
            => type.IsGenericType
               && type.GetGenericTypeDefinition() == typeof(IHandleEvent<>);

        private static bool IsHandler(Type type)
            => type.GetInterfaces().Any(IsHandlerInterface);

        public static IServiceCollection AddPatLite(this IServiceCollection serviceCollection, PatLiteOptions options)
        {
            options.AssemblyScanner?.RegisterHandlers(serviceCollection);

            return serviceCollection
                .AddSingleton(options.SubscriberConfig)
                .RegisterPatLite(options.PolicyBuilder);
        }

        public static IServiceCollection AddPatLite(this IServiceCollection serviceCollection, PatLitePolicyBuilder policyBuilder)
        {
            return serviceCollection.RegisterPatLite(policyBuilder);
        }

        public static IServiceCollection AddPatLite(this IServiceCollection serviceCollection)
        {
            return serviceCollection.RegisterPatLite(null);
        }

        private static IServiceCollection RegisterPatLite(this IServiceCollection serviceCollection, PatLitePolicyBuilder policyBuilder)
        {
            if (policyBuilder == null)
            {
                policyBuilder = new PatLitePolicyBuilder()
                    .AddPolicy<MonitoringPolicy.MonitoringPolicy>()
                    .AddPolicy<StandardPolicy>();
            }
            policyBuilder.RegisterPolicies(serviceCollection);

            serviceCollection.AddTransient<IMessageDependencyResolver, MessageDependencyResolver>()
                .AddTransient<IMessageProcessor, MessageProcessor>()
                .AddScoped<IMessageContext, MessageContext>()
                .AddTransient<ISubscriberPolicy>(policyBuilder.Build)
                .AddSingleton<Subscriber>();

            return serviceCollection;
        }

        public static IServiceCollection AddHandlersFromAssemblyContainingType<T>(this IServiceCollection serviceCollection)
        {
            foreach (var handler in Assembly.GetAssembly(typeof(T)).GetTypes().Where(IsHandler))
            {
                serviceCollection.AddTransient(handler);
            }
            return serviceCollection;
        }
    }
}