using System.Threading.Tasks;
using Pat.Subscriber.IntegrationTests.Helpers;

namespace Pat.Subscriber.IntegrationTests
{
    public class TestEventHandler: IHandleEvent<TestEvent>
    {
        private readonly MessageContext _messageContext;
        private readonly MessageReceivedNotifier<TestEvent> _messageNotifier;

        public TestEventHandler(MessageContext messageContext, MessageReceivedNotifier<TestEvent> messageNotifier)
        {
            _messageContext = messageContext;
            _messageNotifier = messageNotifier;
        }

        public Task HandleAsync(TestEvent message)
        {
            _messageNotifier.OnMessageReceived(new MessageReceivedHandlerArgs<TestEvent>
            {
                CapturedMessage = new CapturedMessage<TestEvent>
                {
                    Message = message,
                    CorrelationId = _messageContext.CorrelationId
                }
            });
            return Task.CompletedTask;
        }
    }
}
