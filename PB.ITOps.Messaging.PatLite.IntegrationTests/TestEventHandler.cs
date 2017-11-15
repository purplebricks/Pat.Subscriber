using System.Collections.Generic;
using System.Threading.Tasks;
using PB.ITOps.Messaging.PatLite.IoC;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests
{
    public class TestEventHandler: IHandleEvent<TestEvent>
    {
        private readonly IMessageContext _messageContext;
        public static List<CapturedEvent<TestEvent>> ReceivedEvents = new List<CapturedEvent<TestEvent>>();

        public TestEventHandler(IMessageContext messageContext)
        {
            _messageContext = messageContext;
        }

        public Task HandleAsync(TestEvent message)
        {
            ReceivedEvents.Add(new CapturedEvent<TestEvent>
            {
                Event = message,
                CorrelationId = _messageContext.CorrelationId
            });
            return Task.CompletedTask;
        }
    }
}
