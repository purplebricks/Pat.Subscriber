using System;
using System.Threading.Tasks;
using Pat.Subscriber.UnitTests.Events;

namespace Pat.Subscriber.UnitTests.Handlers
{
    public class BaseEventHandler : IHandleEvent<Eventv1>
    {
        public Task HandleAsync(Eventv1 message)
        {
            throw new NotImplementedException();
        }
    }
}