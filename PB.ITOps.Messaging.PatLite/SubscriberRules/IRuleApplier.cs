using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite.SubscriberRules
{
    public interface IRuleApplier
    {
        void RemoveRule(RuleDescription rule);
        void AddRule(RuleDescription rule);
    }
}