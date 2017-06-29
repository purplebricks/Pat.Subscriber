using System;
using System.Threading.Tasks;
using PB.ITOps.Messaging.PatLite.UnitTests.Events;

namespace PB.ITOps.Messaging.PatLite.UnitTests.Handlers
{
    public class Event1Handler : IHandleEvent<Event1>
    {
        public Task HandleAsync(Event1 message)
        {
            throw new NotImplementedException();
        }
    }
}