using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite
{
    public class SubscriptionBuilder
    {
        private readonly ILog _log;
        private readonly SubscriberConfig _config;

        public SubscriptionBuilder(ILog log, SubscriberConfig config)
        {
            _log = log;
            _config = config;
        }

        public void Build(string subscriberName, bool usePartitioning, RuleDescription rule, SubscriptionDescription subscriptionDescription, int filterVersion)
        {
            var clientIndex = 1;
            foreach (var connectionString in _config.ConnectionStrings)
            {
                if (!string.IsNullOrEmpty(connectionString))
                {
                    _log.Info($"Buiding subscription {clientIndex}...");
                    BuildSubscription(connectionString, subscriberName, usePartitioning, rule, subscriptionDescription, filterVersion);
                }
                else
                {
                    _log.Info($"Skipping subscription {clientIndex}, connection string is null or empty");
                }
            }
        }

        public async Task<IEnumerable<string>> GetAllSubscribers(string subscriberName)
        {
            var connections = _config.ConnectionStrings;
            var connectionString = connections.First();

            if (!string.IsNullOrEmpty(connectionString))
            {
                var topicName = _config.TopicName;
                var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
                var result = await namespaceManager.GetSubscriptionsAsync(topicName);

                return result.Select(x => x.Name);
            }
            throw new InvalidOperationException("Service bus connection string is undefined. It was impossible to get subscribers list.");
        }

        public RuleDescription CommonSubscriptionRule(string subscriberName, IEnumerable<string> messagesTypeFilters, int filterVersion)
        {
            var coreMessageTypeRule = "MessageType = 'TopicMessageType.Core'";
            var specificSubscriberOrAllRule = $"(NOT EXISTS(SpecificSubscriber) OR SpecificSubscriber = '{subscriberName}')";

            var customMessageTypeRule = $"MessageType IN ('{string.Join("','", messagesTypeFilters)}')";

            var combinedRules = $"({coreMessageTypeRule} OR {customMessageTypeRule}) AND {specificSubscriberOrAllRule}";

            var rule = new RuleDescription($"{subscriberName}_{filterVersion}")
            {
                Filter = new SqlFilter(combinedRules)
            };

            return rule;
        }

        public SubscriptionDescription CommonSubscriptionDescription(string subscriptionName)
        {
            return CommonSubscriptionDescription(_config.TopicName, subscriptionName);
        }

        private void BuildSubscription(string connectionString, string subscriberName, bool usePartitioning, RuleDescription rule, SubscriptionDescription subscriptionDescription, int filterVersion)
        {
            var topicName = _config.TopicName;

            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            if (!namespaceManager.TopicExists(topicName))
            {
                namespaceManager.CreateTopic(new TopicDescription(topicName)
                {
                    EnablePartitioning = usePartitioning
                });
            }

            if (!namespaceManager.SubscriptionExists(topicName, subscriberName))
            {
                namespaceManager.CreateSubscription(subscriptionDescription, rule);
            }
            else
            {
                var existingRules = namespaceManager.GetRules(topicName, subscriberName);

                SubscriptionClient subscriptionClient = null;
                if (!existingRules.Any())
                {
                    _log.Info($"Creating rule {rule.Name} for subscriber {subscriberName}, as it currently does not have any rules");
                    subscriptionClient = SubscriptionClient.CreateFromConnectionString(connectionString, topicName, subscriberName);
                    subscriptionClient.AddRule(rule);
                }
                if (existingRules.Count() == 1)
                {
                    var existingRule = existingRules.First();
                    var existingFilter = (SqlFilter)existingRule.Filter;
                    var existingVersion = 0;

                    if (existingRule.Name.Contains("_"))
                    {
                        existingVersion = int.Parse(existingRule.Name.Split('_')[1]);
                    }

                    var newRule = (SqlFilter)rule.Filter;
                    newRule.Validate();
                    if (existingFilter.SqlExpression != newRule.SqlExpression && existingVersion < filterVersion)
                    {
                        subscriptionClient = SubscriptionClient.CreateFromConnectionString(connectionString, topicName, subscriberName);
                        _log.Info($"Deleting rule {existingRule.Name} for subscriber {subscriberName}, as it has been superceded by a newer version");
                        subscriptionClient.RemoveRule(existingRule.Name);

                        _log.Info($"Creating rule {rule.Name} for subscriber {subscriberName}, as it is a newer version than the one currently on the subscriber");
                        subscriptionClient.AddRule(rule);
                    }
                }
            }
        }

        protected SubscriptionDescription CommonSubscriptionDescription(string topicName, string subscriptionName)
        {
            return new SubscriptionDescription(topicName, subscriptionName)
            {
                DefaultMessageTimeToLive = new TimeSpan(30, 0, 0, 0)
            };
        }
    }
}
