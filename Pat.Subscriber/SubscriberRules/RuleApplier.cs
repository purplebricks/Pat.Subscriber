using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Pat.Subscriber.SubscriberRules
{ 
    public class RuleApplier: IRuleApplier
    {
        private readonly ILogger _log;
        private readonly SubscriptionClient _client;

        public RuleApplier(ILogger log, SubscriptionClient client)
        {
            _log = log;
            _client = client;
        }

        public async Task RemoveRule(RuleDescription rule)
        {
            _log.LogInformation($"Deleting rule {rule.Name} for subscriber {_client.SubscriptionName}, as it has been superceded by a newer version");
            await _client.RemoveRuleAsync(rule.Name).ConfigureAwait(false);
        }

        public async Task AddRule(RuleDescription rule)
        {
            var newRule = (SqlFilter)rule.Filter;
            _log.LogInformation($"Creating rule {rule.Name} for subscriber {_client.SubscriptionName}");
            await _client.AddRuleAsync(rule).ConfigureAwait(false);
        }
    }
}