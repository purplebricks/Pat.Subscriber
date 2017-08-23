using System;
using PB.ITOps.Messaging.PatLite.Serialiser;

namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
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
        public PatLiteGlobalPolicyBuilder GlobalPolicyBuilder { get; set; }
        /// <summary>
        /// Optional
        /// Scanner can be used to register handlers
        /// Alternatively can call helper function ServiceCollection.AddHandlersFromAssemblyContainingType<>
        /// </summary>
        public AssemblyScanner AssemblyScanner { get; set; }
        /// <summary>
        /// Optional
        /// by default DefaultMessageProcessingPolicy will be used
        /// </summary>
        public PatLiteMessagePolicyBuilder MessagePolicyBuilder { get; set; }

        /// <summary>
        /// Optional
        /// by default NewtonsoftMessageDeserialiser will be used
        /// </summary>
        public Func<IServiceProvider, IMessageDeserialiser> MessageDeserialiser { get; set; }
    }
}