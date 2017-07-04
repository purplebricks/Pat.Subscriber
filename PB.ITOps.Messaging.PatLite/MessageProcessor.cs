using System;
using System.Threading.Tasks;
using log4net;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using PB.ITOps.Messaging.PatLite.IoC;
using PB.ITOps.Messaging.PatLite.MessageMapping;
using PB.ITOps.Messaging.PatLite.Policy;

namespace PB.ITOps.Messaging.PatLite
{
    public class MessageProcessor: IMessageProcessor
    {
        private readonly IMessageDependencyResolver _messageDependencyResolver;

        public MessageProcessor(IMessageDependencyResolver messageDependencyResolver)
        {
            _messageDependencyResolver = messageDependencyResolver;
        }

        public async Task ProcessMessage(BrokeredMessage message, ISubscriberPolicy policy)
        {
            using (message)
            {
                using (var scope = _messageDependencyResolver.BeginScope())
                {
                    try
                    {
                        var messageTypeString = message.Properties["MessageType"].ToString();
                        var messageBody = message.GetBody<string>();
                        
                        var ctx = (IMessageContext)scope.GetService(typeof(IMessageContext));
                        var correlationId = message.Properties.ContainsKey("PBCorrelationId")
                            ? message.Properties["PBCorrelationId"].ToString()
                            : Guid.NewGuid().ToString();
                        ctx.CorrelationId = correlationId;
                        LogicalThreadContext.Properties["CorrelationId"] = correlationId;

                        var handlerForMessageType = MessageMapper.GetHandlerForMessageType(messageTypeString);
                        var messageHandler = scope.GetService(handlerForMessageType.HandlerType);

                        var typedMessage = JsonConvert.DeserializeObject(messageBody, handlerForMessageType.MessageType);

                        await (Task)handlerForMessageType.HandlerMethod.Invoke(messageHandler, new [] { typedMessage });

                        policy.OnComplete(message);
                    }
                    catch (Exception ex)
                    {
                        policy.OnFailure(message, ex);
                    }
                }
            }
        }
    }
}
