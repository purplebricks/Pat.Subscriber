using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Core;

namespace Pat.Subscriber
{
    public interface IMultipleBatchProcessor
    {
        Task ProcessMessages(IList<IMessageReceiver> messageReceivers,
            CancellationTokenSource tokenSource);
    }
}