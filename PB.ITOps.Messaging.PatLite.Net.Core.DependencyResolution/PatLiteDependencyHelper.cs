using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.PatLite.BatchProcessing;
using PB.ITOps.Messaging.PatLite.Deserialiser;
using PB.ITOps.Messaging.PatLite.IoC;
using PB.ITOps.Messaging.PatLite.MessageProcessing;
using PB.ITOps.Messaging.PatLite.MonitoringPolicy;

namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
{
    /// <summary>
    /// Helper class to aid registering of PatLite dependencies in Net Core Dependency Injection
    /// </summary>
    public static class PatLiteDependencyHelper
    {
        private static bool IsHandlerInterface(Type type)
            => type.IsGenericType
               && type.GetGenericTypeDefinition() == typeof(IHandleEvent<>);

        private static bool IsHandler(Type type)
            => type.GetInterfaces().Any(IsHandlerInterface);

        /// <summary>
        /// <para>Registers dependencies required for PatLite, allowing customisaton through options</para>
        /// <para>NB: Recommend using parameterless AddPatLite() overload where default batch and message processing is desired</para>
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <param name="options">
        /// <para>Required SubscriberConfiguration</para>
        /// <para>Optional options.BatchMessageProcessingBehaviourPipelineBuilder override for custom batch pipelines, leave as null to build default pipeline</para>
        /// <para>Optional options.MessageProcessingPipelineBuilder override for custom message processing pipelines, leave as null to build default pipeline</para>
        /// </param>
        /// <returns></returns>
        public static IServiceCollection AddPatLite(this IServiceCollection serviceCollection, PatLiteOptions options)
        {
            options.AssemblyScanner?.RegisterHandlers(serviceCollection);

            return serviceCollection
                .AddSingleton(options.SubscriberConfiguration)
                .RegisterPatLite(options.BatchMessageProcessingBehaviourPipelineBuilder, options.MessageProcessingPipelineBuilder, options.MessageDeserialiser);
        }

        /// <summary>
        /// <para>Registers dependencies required for PatLite, allowing customisaton through options</para>
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <param name="subscriberConfiguration"></param>
        /// <returns></returns>
        public static IServiceCollection AddPatLite(this IServiceCollection serviceCollection, SubscriberConfiguration subscriberConfiguration)
        {
            return serviceCollection.AddPatLite(new PatLiteOptions
            {
                SubscriberConfiguration = subscriberConfiguration
            });
        }

        /// <summary>
        /// Registers dependencies required for PatLite building the default message and batch processing pipelines
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <returns></returns>
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
                .AddScoped<MonitoringMessageProcessingBehaviour>()
                .AddScoped<MonitoringBatchProcessingBehaviour>()
                .AddScoped<DefaultBatchProcessingBehaviour>()
                .AddSingleton<MultipleBatchProcessor>()
                .AddSingleton<BatchProcessor>()
                .AddSingleton(provider => new BatchConfiguration(
                    provider.GetService<SubscriberConfiguration>().BatchSize,
                    provider.GetService<SubscriberConfiguration>().ReceiveTimeoutSeconds))
                .AddScoped(provider => provider.GetService<IMessageDependencyResolver>().BeginScope())
                .AddScoped(provider => new MessageContext())
                .AddSingleton(provider => batchMessageProcessingBehaviourBuilder != null ? batchMessageProcessingBehaviourBuilder.Build(provider) :
                    new BatchProcessingBehaviourPipeline()
                        .AddBehaviour<MonitoringBatchProcessingBehaviour>(provider)
                        .AddBehaviour<DefaultBatchProcessingBehaviour>(provider))
                .AddSingleton(provider => messagePipelineDependencyBuilder != null ? messagePipelineDependencyBuilder.Build(provider) :
                    new MessageProcessingBehaviourPipeline()
                        .AddBehaviour<MonitoringMessageProcessingBehaviour>(provider)
                        .AddBehaviour<DefaultMessageProcessingBehaviour>(provider)
                        .AddBehaviour<InvokeHandlerBehaviour>(provider))
                .AddScoped(deserialisationResolver)
                .AddSingleton<Subscriber>();

            return serviceCollection;
        }

        /// <summary>
        /// Helper method that searches assembly for any PatLite handlers and registers them in the service collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceCollection"></param>
        /// <returns></returns>
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