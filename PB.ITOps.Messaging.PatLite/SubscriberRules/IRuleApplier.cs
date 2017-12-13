using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace PB.ITOps.Messaging.PatLite.SubscriberRules
{
    public interface IRuleApplier
    {
        Task RemoveRule(RuleDescription rule);
        Task AddRule(RuleDescription rule);
    }
}