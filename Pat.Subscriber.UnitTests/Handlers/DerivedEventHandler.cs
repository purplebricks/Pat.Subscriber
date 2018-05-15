using System;
using System.Threading.Tasks;
using Pat.Subscriber.UnitTests.Events;

namespace Pat.Subscriber.UnitTests.Handlers
{
    public class DerivedEventHandler : IHandleEvent<Eventv2>
    {
        public Task HandleAsync(Eventv2 message)
        {
            throw new NotImplementedException();
        }
    }
}
