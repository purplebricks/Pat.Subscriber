using System;
using System.Threading.Tasks;
using Pat.Subscriber.UnitTests.Events;

namespace Pat.Subscriber.UnitTests.Handlers
{
    public class Event1Handler : IHandleEvent<Event1>
    {
        public Task HandleAsync(Event1 message)
        {
            throw new NotImplementedException();
        }
    }
}