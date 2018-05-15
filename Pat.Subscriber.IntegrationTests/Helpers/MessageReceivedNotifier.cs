namespace Pat.Subscriber.IntegrationTests.Helpers
{
    public class MessageReceivedNotifier<T>
    {
        public delegate void MessageReceivedHandler(object sender, MessageReceivedHandlerArgs<T> args);

        public event MessageReceivedHandler MessageReceived;

        public virtual void OnMessageReceived(MessageReceivedHandlerArgs<T> args)
        {
            MessageReceived?.Invoke(this, args);
        }
    }
}