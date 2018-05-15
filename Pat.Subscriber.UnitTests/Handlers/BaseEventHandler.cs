using System;
using System.Threading.Tasks;
using PB.ITOps.Messaging.PatLite.UnitTests.Events;

namespace PB.ITOps.Messaging.PatLite.UnitTests.Handlers
{
    public class BaseEventHandler : IHandleEvent<Eventv1>
    {
        public Task HandleAsync(Eventv1 message)
        {
            throw new NotImplementedException();
        }
    }
}