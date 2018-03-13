using System.Collections.Generic;
using System.Threading.Tasks;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests
{
    public class CapturedEvents
    {
        public List<CapturedEvent<TestEvent>> ReceivedEvents = new List<CapturedEvent<TestEvent>>();
    }

    public class TestEventHandler: IHandleEvent<TestEvent>
    {
        private readonly MessageContext _messageContext;
        private readonly CapturedEvents _capturedEvents;

        public TestEventHandler(MessageContext messageContext, CapturedEvents capturedEvents)
        {
            _messageContext = messageContext;
            _capturedEvents = capturedEvents;
        }

        public Task HandleAsync(TestEvent message)
        {
            _capturedEvents.ReceivedEvents.Add(new CapturedEvent<TestEvent>
            {
                Event = message,
                CorrelationId = _messageContext.CorrelationId
            });
            return Task.CompletedTask;
        }
    }
}
