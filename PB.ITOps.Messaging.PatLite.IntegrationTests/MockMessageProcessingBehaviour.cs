using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PB.ITOps.Messaging.PatLite.MessageProcessing;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests
{
    public class MockMessageProcessingBehaviour : IMessageProcessingBehaviour
    {
        public static List<string> CalledForMessages = new List<string>();

        public async Task Invoke(Func<MessageContext, Task> next, MessageContext messageContext)
        {
            CalledForMessages.Add(messageContext.CorrelationId);
            await next(messageContext);
        }
    }
}
