using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Pat.Subscriber
{
    public class Batch
    {
        private readonly IMessageProcessor _messageProcessor;
        private readonly BatchConfiguration _batchConfiguration;
        protected IList<Message> ReceivedMessages;
        private IMessageReceiver _messageReceiver;

        public Batch(IMessageProcessor messageProcessor, BatchConfiguration batchConfiguration)
        {
            _messageProcessor = messageProcessor;
            _batchConfiguration = batchConfiguration;
        }

        public async Task ReceiveMessages(IMessageReceiver messageReceiver)
        {
            _messageReceiver = messageReceiver;
            ReceivedMessages = await _messageReceiver.GetMessages(_batchConfiguration.BatchSize, _batchConfiguration.ReceiveTimeoutSeconds).ConfigureAwait(false);
        }

        public async Task ProcessMessages()
        {
            await Task.WhenAll(ReceivedMessages.Select(m => _messageProcessor.ProcessMessage(m, _messageReceiver)).ToArray()).ConfigureAwait(false);
        }

        public int MessageCount => ReceivedMessages.Count;
        public bool HasMessages => ReceivedMessages.Any();
    }
}