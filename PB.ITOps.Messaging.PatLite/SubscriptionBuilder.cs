using System;
using System.Linq;
using log4net;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using PB.ITOps.Messaging.PatLite.SubscriberRules;

namespace PB.ITOps.Messaging.PatLite
{
    public class SubscriptionBuilder
    {
        private readonly ILog _log;
        private readonly SubscriberConfiguration _config;
        private readonly IRuleVersionResolver _subscriptionRuleVersionResolver;

        public SubscriptionBuilder(ILog log, SubscriberConfiguration config, IRuleVersionResolver subscriptionRuleVersionResolver)
        {
            _log = log;
            _config = config;
            _subscriptionRuleVersionResolver = subscriptionRuleVersionResolver;
        }

        public void Build(SubscriptionDescription subscriptionDescription, string[] messagesTypes, string handlerFullName)
        {
            var clientIndex = 1;
            foreach (var connectionString in _config.ConnectionStrings)
            {
                if (!string.IsNullOrEmpty(connectionString))
                {
                    _log.Info($"Building subscription {clientIndex} on service bus {connectionString.RetrieveServiceBusAddress()}...");
                    BuildSubscription(connectionString, subscriptionDescription, messagesTypes, handlerFullName);
                }
                else
                {
                    _log.Info($"Skipping subscription {clientIndex}, connection string is null or empty");
                }
            }
        }
        
        public SubscriptionDescription CommonSubscriptionDescription()
        {
            return new SubscriptionDescription(_config.EffectiveTopicName, _config.SubscriberName)
            {
                DefaultMessageTimeToLive = new TimeSpan(30, 0, 0, 0)
            };
        }

        private void BuildSubscription(string connectionString, SubscriptionDescription subscriptionDescription, string[] messagesTypes, string handlerFullName)
        {
            var topicName = _config.EffectiveTopicName;

            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            if (!namespaceManager.TopicExists(topicName))
            {
                _log.Info($"Topic '{topicName}' does not exist, creating topic...");
                namespaceManager.CreateTopic(new TopicDescription(topicName)
                {
                    EnablePartitioning = _config.UsePartitioning
                });
            }

            var client = SubscriptionClient.CreateFromConnectionString(connectionString, topicName, _config.SubscriberName);
            RuleBuilder ruleBuilder = new RuleBuilder(new RuleApplier(_log, client), _subscriptionRuleVersionResolver, _config.SubscriberName);

            var newRule = ruleBuilder.GenerateSubscriptionRule(messagesTypes, handlerFullName);

            if (!namespaceManager.SubscriptionExists(topicName, _config.SubscriberName))
            {
                _log.Info($"Subscription '{_config.SubscriberName}' does not exist on topic '{topicName}', creating subscription...");
                namespaceManager.CreateSubscription(subscriptionDescription, newRule);
            }
            else
            {
                _log.Info($"Validating subscription '{_config.SubscriberName}' rules on topic '{topicName}'...");
                var existingRules = namespaceManager.GetRules(topicName, _config.SubscriberName).ToArray();
                ruleBuilder.BuildRules(newRule, existingRules, messagesTypes);
            }
        
        }
    }
}
