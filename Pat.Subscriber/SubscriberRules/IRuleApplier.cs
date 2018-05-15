using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace Pat.Subscriber.SubscriberRules
{
    public interface IRuleApplier
    {
        Task RemoveRule(RuleDescription rule);
        Task AddRule(RuleDescription rule);
    }
}