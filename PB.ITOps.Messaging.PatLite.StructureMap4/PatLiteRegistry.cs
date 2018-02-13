using log4net;
using PB.ITOps.Messaging.PatLite.BatchProcessing;
using PB.ITOps.Messaging.PatLite.IoC;
using PB.ITOps.Messaging.PatLite.MessageProcessing;
using StructureMap;

namespace PB.ITOps.Messaging.PatLite.StructureMap4
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
            For<ILog>().AlwaysUnique().Use(s => LogManager.GetLogger(s.RootType));
        }

        public PatLiteRegistry(PatLiteOptions options): this(options.BatchMessageProcessingBehaviourDependencyBuilder, options.MessageProcessingPipelineDependencyBuilder)
        {
            For<SubscriberConfiguration>().Use(options.SubscriberConfiguration);
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
            }
            else
            {
                messageProcessingPipelineDependencyBuilder.RegisterTypes(this);
                For<MessageProcessingBehaviourPipeline>().Use(context => messageProcessingPipelineDependencyBuilder.Build(context));
            }
           
            For<DefaultMessageProcessingBehaviour>().Use<DefaultMessageProcessingBehaviour>();
            For<InvokeHandlerBehaviour>().Use<InvokeHandlerBehaviour>();
        }
    }
}
