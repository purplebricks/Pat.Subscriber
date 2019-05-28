using System.Reflection;
using System.Threading.Tasks;

namespace Pat.Subscriber
{
    public interface ISubscriptionBuilder
    {
        Task<bool> Build(string[] messagesTypes, string handlerFullName);
        SubscriptionBuilder WithRuleVersionResolver(Assembly[] handlerAssemblies);
    }
}