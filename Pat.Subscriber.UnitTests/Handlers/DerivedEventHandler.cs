using System;
using System.Threading.Tasks;
using PB.ITOps.Messaging.PatLite.UnitTests.Events;

namespace PB.ITOps.Messaging.PatLite.UnitTests.Handlers
{
    public class DerivedEventHandler : IHandleEvent<Eventv2>
    {
        public Task HandleAsync(Eventv2 message)
        {
            throw new NotImplementedException();
        }
    }
}
