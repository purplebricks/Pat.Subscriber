namespace PB.ITOps.Messaging.PatLite
{
    public class BatchFactory
    {
        private readonly IMessageProcessor _messageProcessor;
        private readonly BatchConfiguration _batchConfiguration;

        public BatchFactory(IMessageProcessor messageProcessor, BatchConfiguration batchConfiguration)
        {
            _messageProcessor = messageProcessor;
            _batchConfiguration = batchConfiguration;
        }
        public Batch Create()
        {
            return new Batch(_messageProcessor, _batchConfiguration);
        }
    }
}