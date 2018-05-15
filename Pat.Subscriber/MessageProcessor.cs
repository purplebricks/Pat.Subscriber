using System;
using System.Threading.Tasks;
using log4net;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Pat.Subscriber.IoC;
using Pat.Subscriber.MessageProcessing;

namespace Pat.Subscriber
{
    public class MessageProcessor: IMessageProcessor
    {
        private readonly IMessageDependencyResolver _messageDependencyResolver;
        private readonly MessageProcessingBehaviourPipeline _pipeline;

        public MessageProcessor(IMessageDependencyResolver messageDependencyResolver, MessageProcessingBehaviourPipeline pipeline)
        {
            _messageDependencyResolver = messageDependencyResolver;
            _pipeline = pipeline;
        }

        public async Task ProcessMessage(Message message, IMessageReceiver messageReceiver)
        {
            using (var scope = _messageDependencyResolver.BeginScope())
            {
                var ctx = (MessageContext)scope.GetService(typeof(MessageContext));
                ctx.CorrelationId = message.UserProperties.ContainsKey("PBCorrelationId")
                    ? message.UserProperties["PBCorrelationId"].ToString()
                    : Guid.NewGuid().ToString();
                ctx.MessageEncrypted = message.UserProperties.ContainsKey("Encrypted") && bool.Parse(message.UserProperties["Encrypted"].ToString());
                ctx.Synthetic = message.UserProperties.ContainsKey("Synthetic") && bool.Parse(message.UserProperties["Synthetic"].ToString());
                ctx.DomainUnderTest = message.UserProperties.ContainsKey("DomainUnderTest") ? message.UserProperties["DomainUnderTest"].ToString() : null;
                ctx.MessageReceiver = messageReceiver;
                ctx.DependencyScope = scope;
                ctx.Message = message;

                LogicalThreadContext.Properties["CorrelationId"] = ctx.CorrelationId;

                await _pipeline.Invoke(ctx).ConfigureAwait(false);
            }
        }
    }
}
