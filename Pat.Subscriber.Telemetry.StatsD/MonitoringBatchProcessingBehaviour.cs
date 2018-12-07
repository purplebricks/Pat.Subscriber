using System;
using System.Threading.Tasks;
using Pat.Subscriber.BatchProcessing;

namespace Pat.Subscriber.Telemetry.StatsD
{
    public class MonitoringBatchProcessingBehaviour: IBatchProcessingBehaviour
    {
        private readonly SubscriberConfiguration _config;
        private readonly IStatisticsReporter _statisticsReporter;

        public MonitoringBatchProcessingBehaviour(SubscriberConfiguration config, IStatisticsReporter statisticsReporter)
        {
            _config = config;
            _statisticsReporter = statisticsReporter;
        }

        public async Task Invoke(Func<BatchContext, Task> next, BatchContext context)
        {
            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _statisticsReporter.Increment("ProcessBatchInfrastructureException",
                    $"Client=PatLite.{_config.SubscriberName}," +
                    $"ExceptionType={ex.GetType()}");
                throw;
            }
        }
    }
}
