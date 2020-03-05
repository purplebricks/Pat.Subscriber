using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Logging;
using Pat.Subscriber.SubscriberRules;

namespace Pat.Subscriber
{
    public class SubscriptionBuilder : ISubscriptionBuilder
    {
        private readonly ILogger _log;
        private readonly ILogger<RuleApplier> _ruleApplierLog;
        private readonly SubscriberConfiguration _config;
        private IRuleVersionResolver _subscriptionRuleVersionResolver;

        public SubscriptionBuilder(ILogger<SubscriptionBuilder> log, ILogger<RuleApplier> ruleApplierLog, SubscriberConfiguration config)
        {
            _log = log;
            _ruleApplierLog = ruleApplierLog;
            _config = config;
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
                        _log.LogCritical($"Service bus timeout, probable cause is a missing servicebus subscription called '{_config.SubscriberName}'. Subscriber will terminate.");
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

            SubscriptionClient client;
            if (_config.TokenProvider != null)
            {
                ServiceBusConnectionStringBuilder builder = new ServiceBusConnectionStringBuilder(connectionString);
                client = new SubscriptionClient(builder.Endpoint, topicName, _config.SubscriberName, _config.TokenProvider);
            }
            else 
            {    
                client = new SubscriptionClient(connectionString, topicName, _config.SubscriberName);
            }
            

            var ruleApplier = new RuleApplier(_ruleApplierLog, client);
            var ruleBuilder = new RuleBuilder(ruleApplier, _subscriptionRuleVersionResolver, _config.SubscriberName);

            var rulesForCurrentSoftwareVersion = ruleBuilder.GenerateSubscriptionRules(messagesTypes, handlerFullName, _config.OmitSpecificSubscriberFilter).ToArray();
            var rulesCurrentlyDefinedInServiceBus = await client.GetRulesAsync().ConfigureAwait(false);

            _log.LogInformation($"Validating subscription '{_config.SubscriberName}' rules on topic '{topicName}'...");
            await ruleBuilder.ApplyRuleChanges(rulesForCurrentSoftwareVersion, rulesCurrentlyDefinedInServiceBus.ToArray(), messagesTypes).ConfigureAwait(false);
        }

        public SubscriptionBuilder WithRuleVersionResolver(Assembly[] handlerAssemblies)
        {
            _subscriptionRuleVersionResolver = new RuleVersionResolver(handlerAssemblies);
            return this;
        }
    }
}
