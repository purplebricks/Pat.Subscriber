using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pat.Subscriber.BatchProcessing;
using Pat.Subscriber.CicuitBreaker;
using Pat.Subscriber.Deserialiser;
using Pat.Subscriber.IoC;
using Pat.Subscriber.MessageProcessing;

namespace Pat.Subscriber.NetCoreDependencyResolution
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
                .RegisterPatLite(options.BatchMessageProcessingBehaviourPipelineBuilder, 
                    options.MessageProcessingPipelineBuilder, 
                    options.MessageDeserialiser,
                    options.CircuitBreakerOptions);
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
            return serviceCollection.RegisterPatLite(null, null, null, null);
        }
        
        private static IServiceCollection RegisterPatLite(
            this IServiceCollection serviceCollection,
            BatchPipelineDependencyBuilder batchMessageProcessingBehaviourBuilder, 
            MessagePipelineDependencyBuilder messagePipelineDependencyBuilder,
            Func<IServiceProvider, IMessageDeserialiser> messageDeserialiser,
            Func<IServiceProvider, CircuitBreakerBatchProcessingBehaviour.CircuitBreakerOptions> circuitBreakerOptions)
        {
            messagePipelineDependencyBuilder?.RegisterTypes(serviceCollection);
            batchMessageProcessingBehaviourBuilder?.RegisterTypes(serviceCollection);

            var deserialisationResolver = messageDeserialiser ?? (provider => new NewtonsoftMessageDeserialiser());

            if (circuitBreakerOptions != null)
            {
                serviceCollection.AddSingleton(circuitBreakerOptions);
            }

            serviceCollection.AddSingleton<IMessageDependencyResolver, MessageDependencyResolver>()
                .AddSingleton<IMessageProcessor, MessageProcessor>()
                .AddSingleton<DefaultMessageProcessingBehaviour>()
                .AddSingleton<InvokeHandlerBehaviour>()
                .AddSingleton<DefaultBatchProcessingBehaviour>()
                .AddSingleton<IMultipleBatchProcessor>(provider => 
                    new MultipleBatchProcessor(
                        provider.GetService<BatchProcessor>(),
                        provider.GetService<ILogger<MultipleBatchProcessor>>(),
                        provider.GetService<SubscriberConfiguration>().SubscriberName))
                .AddSingleton<IMessageReceiverFactory, AzureServiceBusMessageReceiverFactory>()
                .AddTransient<ISubscriptionBuilder, SubscriptionBuilder>()
                .AddSingleton<BatchFactory>()
                .AddSingleton<BatchProcessor>()
                .AddSingleton(provider => new BatchConfiguration(
                    provider.GetService<SubscriberConfiguration>().BatchSize,
                    provider.GetService<SubscriberConfiguration>().ReceiveTimeoutSeconds))
                .AddScoped(provider => provider.GetService<IMessageDependencyResolver>().BeginScope())
                .AddScoped(provider => new MessageContext())
                .AddSingleton(provider => batchMessageProcessingBehaviourBuilder != null ? batchMessageProcessingBehaviourBuilder.Build(provider) :
                    new BatchProcessingBehaviourPipeline()
                        .AddBehaviour<DefaultBatchProcessingBehaviour>(provider))
                .AddSingleton(provider => messagePipelineDependencyBuilder != null ? messagePipelineDependencyBuilder.Build(provider) :
                    new MessageProcessingBehaviourPipeline()
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