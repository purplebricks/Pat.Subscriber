using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.PatLite.GlobalSubscriberPolicy;
using PB.ITOps.Messaging.PatLite.IoC;
using PB.ITOps.Messaging.PatLite.MessageProcessingPolicy;
using PB.ITOps.Messaging.PatLite.Serialiser;

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
                .AddSingleton(options.SubscriberConfiguration)
                .RegisterPatLite(options.GlobalPolicyBuilder, options.MessagePolicyBuilder, options.MessageDeserialiser);
        }

        public static IServiceCollection AddPatLite(this IServiceCollection serviceCollection, PatLiteGlobalPolicyBuilder globalPolicyBuilder, PatLiteMessagePolicyBuilder messagePolicyBuilder)
        {
            return serviceCollection.RegisterPatLite(globalPolicyBuilder, messagePolicyBuilder, null);
        }

        public static IServiceCollection AddPatLite(this IServiceCollection serviceCollection)
        {
            return serviceCollection.RegisterPatLite(null, null, null);
        }

        private static IServiceCollection RegisterPatLite(
            this IServiceCollection serviceCollection,
            PatLiteGlobalPolicyBuilder globalPolicyBuilder, 
            PatLiteMessagePolicyBuilder messagePolicyBuilder,
            Func<IServiceProvider, IMessageDeserialiser> messageDeserialiser)
        {
            if (globalPolicyBuilder == null)
            {
                globalPolicyBuilder = new PatLiteGlobalPolicyBuilder()
                    .AddPolicy<MonitoringPolicy.MonitoringPolicy>()
                    .AddPolicy<StandardPolicy>();
            }

            if (messagePolicyBuilder == null)
            {
                messagePolicyBuilder = new PatLiteMessagePolicyBuilder()
                    .AddPolicy<DefaultMessageProcessingPolicy>();
            }

            messagePolicyBuilder.RegisterPolicies(serviceCollection);
            globalPolicyBuilder.RegisterPolicies(serviceCollection);
            var deserialisationResolver = messageDeserialiser ?? (provider => new NewtonsoftMessageDeserialiser());

            serviceCollection.AddTransient<IMessageDependencyResolver, MessageDependencyResolver>()
                .AddTransient<IMessageProcessor, MessageProcessor>()
                .AddScoped<IMessageContext, MessageContext>()
                .AddTransient(globalPolicyBuilder.Build)
                .AddScoped(messagePolicyBuilder.Build)
                .AddScoped(deserialisationResolver)
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