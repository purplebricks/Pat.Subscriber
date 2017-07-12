using System.Threading.Tasks;
using PB.ITOps.Messaging.PatLite;
using PB.ITOps.Messaging.PatSender;

namespace TestSubscriber
{
    public class EventToPublish
    {
        
    }
    public class RightmoveHandler : IHandleEvent<MyEvent1>, IHandleEvent<MyEvent2>, IHandleEvent<MyEvent1AllNew>
    {
        private readonly IMessagePublisher _messagePublisher;

        public RightmoveHandler(IMessagePublisher messagePublisher)
        {
            _messagePublisher = messagePublisher;
        }

        public async Task HandleAsync(MyEvent1 message)
        {
            await _messagePublisher.PublishEvent(new EventToPublish());
        }

        public Task HandleAsync(MyEvent2 message)
        {
            return Task.CompletedTask;
        }

        public Task HandleAsync(MyDerivedEvent2 message)
        {
            throw new System.NotImplementedException();
        }

        public Task HandleAsync(MyEvent1AllNew message)
        {
            throw new System.NotImplementedException();
        }
    }
}