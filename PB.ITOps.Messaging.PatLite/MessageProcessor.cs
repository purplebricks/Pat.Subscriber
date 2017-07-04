using System;
using System.Reflection;
using System.Threading.Tasks;
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
                    var messageTypeString = "";
                    var messageBody = "";
                    try
                    {
                        messageTypeString = message.Properties["MessageType"].ToString();
                        messageBody = message.GetBody<string>();

                        var ctx = (IMessageContext)scope.GetService(typeof(IMessageContext));
                        ctx.CorrelationId = message.Properties.ContainsKey("PBCorrelationId")
                            ? message.Properties["PBCorrelationId"].ToString()
                            : Guid.NewGuid().ToString();

                        var handlerForMessageType = MessageMapper.GetHandlerForMessageType(messageTypeString);
                        var messageHandler = scope.GetService(handlerForMessageType.HandlerType);

                        var typedMessage = JsonConvert.DeserializeObject(messageBody, handlerForMessageType.MessageType);

                        await (Task)handlerForMessageType.HandlerMethod.Invoke(messageHandler, new [] { typedMessage });

                        //log
                        policy.OnComplete(message);
                        //SendResultToStatsD(_subscriberName, messageType, result.Status.ToString());
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
