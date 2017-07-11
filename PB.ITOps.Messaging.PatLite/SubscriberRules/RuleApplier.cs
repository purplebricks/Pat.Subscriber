using log4net;
using Microsoft.ServiceBus.Messaging;

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

        public void RemoveRule(RuleDescription rule)
        {
            _log.Info($"Deleting rule {rule.Name} for subscriber {_client.Name}, as it has been superceded by a newer version");
            _client.RemoveRule(rule.Name);
        }

        public void AddRule(RuleDescription rule)
        {
            var newRule = (SqlFilter)rule.Filter;
            newRule.Validate();
            _log.Info($"Creating rule {rule.Name} for subscriber {_client.Name}");
            _client.AddRule(rule);
        }
    }
}