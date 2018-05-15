using System;
using System.Threading.Tasks;
using Pat.Subscriber.UnitTests.Events;

namespace Pat.Subscriber.UnitTests.Handlers
{
    public class Event1And2Handler : IHandleEvent<Event1>, IHandleEvent<Event2>
    {
        public Task HandleAsync(Event1 message)
        {
            throw new NotImplementedException();
        }

        public Task HandleAsync(Event2 message)
        {
            throw new NotImplementedException();
        }
    }
}