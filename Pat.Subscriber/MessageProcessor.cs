using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;
using Pat.Subscriber.IoC;
using Pat.Subscriber.MessageProcessing;

namespace Pat.Subscriber
{
    public class MessageProcessor: IMessageProcessor
    {
        private readonly IMessageDependencyResolver _messageDependencyResolver;
        private readonly MessageProcessingBehaviourPipeline _pipeline;
        private readonly ILogger logger;

        public MessageProcessor(IMessageDependencyResolver messageDependencyResolver, MessageProcessingBehaviourPipeline pipeline, ILogger logger)
        {
            _messageDependencyResolver = messageDependencyResolver;
            _pipeline = pipeline;
            this.logger = logger;
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

                using (logger.BeginScope(ctx.CorrelationId))
                {
                    await _pipeline.Invoke(ctx).ConfigureAwait(false);
                }
            }
        }
    }
}
