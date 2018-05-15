using System;
using Pat.Subscriber.Deserialiser;

namespace Pat.Subscriber.DataProtectionDecryption.NetCoreDependencyResolution
{
    public class PatLiteOptions
    {
        /// <summary>
        /// Required
        /// </summary>
        public SubscriberConfiguration SubscriberConfiguration { get; set; }
        /// <summary>
        /// Optional 
        /// by default monitoring and standard processing polciies will be used
        /// </summary>
        public BatchPipelineDependencyBuilder BatchMessageProcessingBehaviourPipelineBuilder { get; set; }

        /// <summary>
        /// Optional
        /// By default standard message processing pipeline will be used with default message processing behaviour
        /// </summary>
        public MessagePipelineDependencyBuilder MessageProcessingPipelineBuilder { get; set; }
        /// <summary>
        /// Optional
        /// Scanner can be used to register handlers
        /// Alternatively can call helper function ServiceCollection.AddHandlersFromAssemblyContainingType&lt;T&gt;
        /// </summary>
        public AssemblyScanner AssemblyScanner { get; set; }

        /// <summary>
        /// Optional
        /// by default NewtonsoftMessageDeserialiser will be used
        /// </summary>
        public Func<IServiceProvider, IMessageDeserialiser> MessageDeserialiser { get; set; }
    }
}