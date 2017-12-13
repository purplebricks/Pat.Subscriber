using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.PatLite.BatchProcessing;
using PB.ITOps.Messaging.PatLite.Deserialiser;
using PB.ITOps.Messaging.PatLite.IoC;
using PB.ITOps.Messaging.PatLite.MessageProcessing;

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
                .RegisterPatLite(options.BatchMessageProcessingBehaviourPipeline, options.MessageProcessingPipeline, options.MessageDeserialiser);
        }

        public static IServiceCollection AddPatLite(this IServiceCollection serviceCollection)
        {
            return serviceCollection.RegisterPatLite(null, null, null);
        }

        private static IServiceCollection RegisterPatLite(
            this IServiceCollection serviceCollection,
            BatchPipelineDependencyBuilder batchMessageProcessingBehaviourBuilder, 
            MessagePipelineDependencyBuilder messagePipelineDependencyBuilder,
            Func<IServiceProvider, IMessageDeserialiser> messageDeserialiser)
        {

            messagePipelineDependencyBuilder?.RegisterTypes(serviceCollection);
            batchMessageProcessingBehaviourBuilder?.RegisterTypes(serviceCollection);
            var deserialisationResolver = messageDeserialiser ?? (provider => new NewtonsoftMessageDeserialiser());

            serviceCollection.AddTransient<IMessageDependencyResolver, MessageDependencyResolver>()
                .AddTransient<IMessageProcessor, MessageProcessor>()
                .AddScoped<DefaultMessageProcessingBehaviour>()
                .AddScoped<InvokeHandlerBehaviour>()
                .AddScoped(provider => provider.GetService<IMessageDependencyResolver>().BeginScope())
                .AddScoped(provider => new MessageContext())
                .AddSingleton(provider => batchMessageProcessingBehaviourBuilder != null ? batchMessageProcessingBehaviourBuilder.Build(provider) :
                    new BatchProcessingBehaviourPipeline()
                        .AddBehaviour<MonitoringPolicy.MonitoringBatchProcessingBehaviour>(provider)
                        .AddBehaviour<DefaultBatchProcessingBehaviour>(provider))
                .AddSingleton(provider => messagePipelineDependencyBuilder != null ? messagePipelineDependencyBuilder.Build(provider) :
                    new MessageProcessingBehaviourPipeline()
                        .AddBehaviour<DefaultMessageProcessingBehaviour>(provider)
                        .AddBehaviour<InvokeHandlerBehaviour>(provider))
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