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
            var ruleApplier = new RuleApplier(_log, client);

            var ruleBuilder = new RuleBuilder(ruleApplier, _subscriptionRuleVersionResolver, _config.SubscriberName);

            var rulesForCurrentSoftwareVersion = ruleBuilder.GenerateSubscriptionRules(messagesTypes, handlerFullName).ToArray();

            if (!namespaceManager.SubscriptionExists(topicName, _config.SubscriberName))
            {
                _log.Info($"Subscription '{_config.SubscriberName}' does not exist on topic '{topicName}', creating subscription...");
                CreateSubscriptionReadyToReceiveRules(subscriptionDescription, namespaceManager);
            }

            _log.Info($"Validating subscription '{_config.SubscriberName}' rules on topic '{topicName}'...");
            var rulesCurrentlyDefinedInServiceBus = namespaceManager.GetRules(topicName, _config.SubscriberName).ToArray();

            ruleBuilder.ApplyRuleChanges(rulesForCurrentSoftwareVersion, rulesCurrentlyDefinedInServiceBus, messagesTypes);
        }

        private static void CreateSubscriptionReadyToReceiveRules(SubscriptionDescription subscriptionDescription, NamespaceManager namespaceManager)
        {
            // Create a temporary rule description because this API doesn't allow us to create many rules when the subscription
            // is created.
            namespaceManager.CreateSubscription(subscriptionDescription, new RuleDescription("$Default", new SqlFilter("1=0")));
        }
    }
}
