using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PB.ITOps.Messaging.PatLite.BatchProcessing;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests
{
    public class MockBatchProcessingBehaviour : IBatchProcessingBehaviour
    {
        private readonly MockBatchProcessingBehaviourSettings _mockBatchProcessingBehaviourSettings;

        public class MockBatchProcessingBehaviourSettings
        {
            public string CorrelationId { get; set; }
        }
        public static List<string> CalledForMessages = new List<string>();

        public MockBatchProcessingBehaviour(MockBatchProcessingBehaviourSettings mockBatchProcessingBehaviourSettings)
        {
            _mockBatchProcessingBehaviourSettings = mockBatchProcessingBehaviourSettings;
        }

        public async Task Invoke(Func<BatchContext, Task> next, BatchContext context)
        {
            CalledForMessages.Add(_mockBatchProcessingBehaviourSettings.CorrelationId);
            await next(context);
        }
    }
}