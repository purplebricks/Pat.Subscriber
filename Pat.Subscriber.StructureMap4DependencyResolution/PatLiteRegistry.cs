﻿using Microsoft.Extensions.Logging;
using Pat.Subscriber.BatchProcessing;
using Pat.Subscriber.Deserialiser;
using Pat.Subscriber.IoC;
using Pat.Subscriber.MessageProcessing;
using Pat.Subscriber.StructureMap4DependencyResolution.Logging;
using StructureMap;

namespace Pat.Subscriber.StructureMap4DependencyResolution
{
    public class PatLiteRegistry : Registry
    {
        private void CommonRegistrySetup()
        {
            Scan(scanner =>
            {
                scanner.WithDefaultConventions();
                scanner.AssemblyContainingType<MessageContext>();
            });

            For<IMessageDependencyResolver>().Use<StructureMapDependencyResolver>();
        }

        public PatLiteRegistry(PatLiteOptions options): this(options.BatchMessageProcessingBehaviourDependencyBuilder, options.MessageProcessingPipelineDependencyBuilder)
        {
            For<SubscriberConfiguration>().Use(options.SubscriberConfiguration);
            For<IMessageDeserialiser>().Use(context => options.MessageDeserialiser(context));
            For(typeof(ILogger<>)).Use(new LoggerInstanceFactory());
        }

        private PatLiteRegistry(BatchPipelineDependencyBuilder batchMessageProcessingBehaviourPipelineDependencyBuilder,
            MessagePipelineDependencyBuilder messageProcessingPipelineDependencyBuilder)
        {
            CommonRegistrySetup();

            if (batchMessageProcessingBehaviourPipelineDependencyBuilder == null)
            {
                For<BatchProcessingBehaviourPipeline>().Use((ctx) =>
                    new BatchProcessingBehaviourPipeline()
                        .AddBehaviour<DefaultBatchProcessingBehaviour>(ctx)
                );
            }
            else
            {
                batchMessageProcessingBehaviourPipelineDependencyBuilder.RegisterTypes(this);
                For<BatchProcessingBehaviourPipeline>().Use(context => batchMessageProcessingBehaviourPipelineDependencyBuilder.Build(context));
            }

            if (messageProcessingPipelineDependencyBuilder == null)
            {
                For<MessageProcessingBehaviourPipeline>().Use((ctx) =>
                    new MessageProcessingBehaviourPipeline()
                        .AddBehaviour<DefaultMessageProcessingBehaviour>(ctx)
                        .AddBehaviour<InvokeHandlerBehaviour>(ctx));

                For<DefaultMessageProcessingBehaviour>().Use<DefaultMessageProcessingBehaviour>();
                For<InvokeHandlerBehaviour>().Use<InvokeHandlerBehaviour>();
            }
            else
            {
                messageProcessingPipelineDependencyBuilder.RegisterTypes(this);
                For<MessageProcessingBehaviourPipeline>().Use(context => messageProcessingPipelineDependencyBuilder.Build(context));
            }
          
            For<IMultipleBatchProcessor>().Use<MultipleBatchProcessor>().Ctor<string>().Is(context => context.GetInstance<SubscriberConfiguration>().SubscriberName);
            For<BatchProcessor>().Use<BatchProcessor>();
            For<BatchFactory>().Use<BatchFactory>();
            For<IMessageReceiverFactory>().Use<AzureServiceBusMessageReceiverFactory>();
            For<ISubscriptionBuilder>().Use<SubscriptionBuilder>();
            For<BatchConfiguration>().Use(context => new BatchConfiguration(
                    context.GetInstance<SubscriberConfiguration>().BatchSize,
                    context.GetInstance<SubscriberConfiguration>().ReceiveTimeoutSeconds));
        }
    }
}
