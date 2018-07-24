using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Pat.Subscriber.SubscriberRules;

namespace Pat.Subscriber
{
    public class SubscriptionBuilder
    {
        private readonly ILogger _log;
        private readonly SubscriberConfiguration _config;
        private readonly IRuleVersionResolver _subscriptionRuleVersionResolver;

        public SubscriptionBuilder(ILogger log, SubscriberConfiguration config, IRuleVersionResolver subscriptionRuleVersionResolver)
        {
            _log = log;
            _config = config;
            _subscriptionRuleVersionResolver = subscriptionRuleVersionResolver;
        }

        public async Task<bool> Build(string[] messagesTypes, string handlerFullName)
        {
            var clientIndex = 1;
            foreach (var connectionString in _config.ConnectionStrings)
            {
                if (!string.IsNullOrEmpty(connectionString))
                {
                    _log.LogInformation($"Building subscription {clientIndex} on service bus {connectionString.RetrieveServiceBusAddress()}...");
                    try
                    {
                        await BuildSubscription(connectionString, messagesTypes, handlerFullName).ConfigureAwait(false);
                    }
                    catch (ServiceBusTimeoutException)
                    {
                        _log.LogCritical($"Service bus timeout, probable cause is a missing servicebus subscription called '{_config.SubscriberName}'. Subcriber will terminate.");
                        return false;
                    }
                    catch (MessagingEntityNotFoundException)
                    {
                        _log.LogCritical($"Unable to find servicebus topic '{_config.EffectiveTopicName}' subscriber will terminate.");
                        return false;
                    }
                }
                else
                {
                    _log.LogInformation($"Skipping subscription {clientIndex}, connection string is null or empty");
                }
            }
            return true;
        }
        
        private async Task BuildSubscription(string connectionString, string[] messagesTypes, string handlerFullName)
        {
            var topicName = _config.EffectiveTopicName;

            var client = new SubscriptionClient(connectionString, topicName, _config.SubscriberName);
         
            var ruleApplier = new RuleApplier(_log, client);
            var ruleBuilder = new RuleBuilder(ruleApplier, _subscriptionRuleVersionResolver, _config.SubscriberName);

            var rulesForCurrentSoftwareVersion = ruleBuilder.GenerateSubscriptionRules(messagesTypes, handlerFullName).ToArray();
            var rulesCurrentlyDefinedInServiceBus = await client.GetRulesAsync().ConfigureAwait(false);

            _log.LogInformation($"Validating subscription '{_config.SubscriberName}' rules on topic '{topicName}'...");
            await ruleBuilder.ApplyRuleChanges(rulesForCurrentSoftwareVersion, rulesCurrentlyDefinedInServiceBus.ToArray(), messagesTypes).ConfigureAwait(false);
        }
    }
}