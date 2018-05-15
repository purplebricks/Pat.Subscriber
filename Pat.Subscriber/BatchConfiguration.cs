namespace Pat.Subscriber
{
    public class BatchConfiguration
    {
        public int BatchSize { get; }
        public int ReceiveTimeoutSeconds { get; }

        public BatchConfiguration(int batchSize, int receiveTimeoutSeconds)
        {
            ReceiveTimeoutSeconds = receiveTimeoutSeconds;
            BatchSize = batchSize;
        }
    }
}