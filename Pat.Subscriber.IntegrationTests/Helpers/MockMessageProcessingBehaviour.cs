using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pat.Subscriber.MessageProcessing;

namespace Pat.Subscriber.IntegrationTests.Helpers
{
    public class MockMessageProcessingBehaviour : IMessageProcessingBehaviour
    {
        public static List<string> CalledForMessages = new List<string>();

        public async Task Invoke(Func<MessageContext, Task> next, MessageContext messageContext)
        {
            CalledForMessages.Add(messageContext.CorrelationId);
            await next(messageContext).ConfigureAwait(false);
        }
    }
}
