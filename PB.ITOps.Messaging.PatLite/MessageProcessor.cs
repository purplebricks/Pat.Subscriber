using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using PB.ITOps.Messaging.PatLite.IoC;
using PB.ITOps.Messaging.PatLite.MessageMapping;

namespace PB.ITOps.Messaging.PatLite
{
    public class MessageProcessor: IMessageProcessor
    {
        private readonly IMessageDependencyResolver _messageDependencyResolver;

        public MessageProcessor(IMessageDependencyResolver messageDependencyResolver)
        {
            _messageDependencyResolver = messageDependencyResolver;
        }

        public async Task  ProcessMessage(BrokeredMessage message)
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
                        message.Complete();
                        //SendResultToStatsD(_subscriberName, messageType, result.Status.ToString());
                    }
                    catch (Exception exception)
                    {
                        //message.SendResultToStatsD(_subscriberName, messageTypeString, "UnhandledException");
                        if (exception is TargetInvocationException && exception.InnerException != null)
                        {
                            exception = exception.InnerException;
                        }

                        var aggregateException = exception as AggregateException;
                        if (aggregateException != null)
                        {
                            var flattened = aggregateException.Flatten();
                            exception = flattened.InnerException ?? flattened;
                        }

                        //_messageProcessingPolicy.Fail(exception, message);

                        //message.LogException(_log, _subscriber.SubscriberName, _logFullMessageBody, messageBody, exception);
                    }
                }
            }
        }
    }
}
