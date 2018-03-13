using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace PB.ITOps.Messaging.PatLite
{
    public class MessageCollection
    {
        private readonly ICollection<Message> _receivedMessages;
        private readonly IMessageReceiver _messageReceiver;

        public static async Task<MessageCollection> ReceiveMessages(IMessageReceiver messageReceiver, int batchSize, int receiveTimeoutSeconds)
        {
            var messages = await messageReceiver.GetMessages(batchSize, receiveTimeoutSeconds).ConfigureAwait(false);
            return new MessageCollection(messages, messageReceiver);
        }

        private MessageCollection(ICollection<Message> receivedMessages, IMessageReceiver messageReceiver)
        {
            _receivedMessages = receivedMessages;
            _messageReceiver = messageReceiver;
        }

        public Task Process(IMessageProcessor messageProcessor)
        {
            return Task.WhenAll(_receivedMessages.Select(m => messageProcessor.ProcessMessage(m, _messageReceiver)).ToArray());
        }

        public int Count => _receivedMessages.Count;
        public bool Any => _receivedMessages.Any();
    }
}