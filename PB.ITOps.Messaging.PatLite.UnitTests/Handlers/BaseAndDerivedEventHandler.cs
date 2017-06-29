using System;
using System.Threading.Tasks;
using PB.ITOps.Messaging.PatLite.UnitTests.Events;

namespace PB.ITOps.Messaging.PatLite.UnitTests.Handlers
{
    public class BaseAndDerivedEventHandler : IHandleEvent<Eventv1>, IHandleEvent<Eventv2>
    {
        public Task HandleAsync(Eventv1 message)
        {
            throw new NotImplementedException();
        }

        public Task HandleAsync(Eventv2 message)
        {
            throw new NotImplementedException();
        }
    }
}