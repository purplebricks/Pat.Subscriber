using System.Threading.Tasks;
using log4net;
using Microsoft.Azure.ServiceBus;

namespace PB.ITOps.Messaging.PatLite.SubscriberRules
{ 
    public class RuleApplier: IRuleApplier
    {
        private readonly ILog _log;
        private readonly SubscriptionClient _client;

        public RuleApplier(ILog log, SubscriptionClient client)
        {
            _log = log;
            _client = client;
        }

        public async Task RemoveRule(RuleDescription rule)
        {
            _log.Info($"Deleting rule {rule.Name} for subscriber {_client.SubscriptionName}, as it has been superceded by a newer version");
            await _client.RemoveRuleAsync(rule.Name).ConfigureAwait(false);
        }

        public async Task AddRule(RuleDescription rule)
        {
            var newRule = (SqlFilter)rule.Filter;
            _log.Info($"Creating rule {rule.Name} for subscriber {_client.SubscriptionName}");
            await _client.AddRuleAsync(rule).ConfigureAwait(false);
        }
    }
}