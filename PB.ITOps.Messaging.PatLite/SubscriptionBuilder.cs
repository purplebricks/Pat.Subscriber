using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private readonly Version _version;

        public SubscriptionBuilder(ILog log, SubscriberConfig config)
        {
            _log = log;
            _config = config;
            _version = Assembly.GetExecutingAssembly().GetName().Version;
        }

        public void Build(RuleDescription rule, SubscriptionDescription subscriptionDescription)
        {
            var clientIndex = 1;
            foreach (var connectionString in _config.ConnectionStrings)
            {
                if (!string.IsNullOrEmpty(connectionString))
                {
                    _log.Info($"Buiding subscription {clientIndex}...");
                    BuildSubscription(connectionString, rule, subscriptionDescription);
                }
                else
                {
                    _log.Info($"Skipping subscription {clientIndex}, connection string is null or empty");
                }
            }
        }
        
        public RuleDescription SubscriptionRule(IEnumerable<string> messagesTypeFilters)
        {
            var specificSubscriberOrAllRule = $"(NOT EXISTS(SpecificSubscriber) OR SpecificSubscriber = '{_config.SubscriberName}')";
            var customMessageTypeRule = $"MessageType IN ('{string.Join("','", messagesTypeFilters)}')";
            var combinedRules = $"({customMessageTypeRule}) AND {specificSubscriberOrAllRule}";

            var rule = new RuleDescription($"{_config.SubscriberName}_{_version.Major}_{_version.Minor}_{_version.Build}")
            {
                Filter = new SqlFilter(combinedRules)
            };

            return rule;
        }
        public SubscriptionDescription CommonSubscriptionDescription()
        {
            return new SubscriptionDescription(_config.TopicName, _config.SubscriberName)
            {
                DefaultMessageTimeToLive = new TimeSpan(30, 0, 0, 0)
            };
        }

        private void BuildSubscription(string connectionString, RuleDescription rule, SubscriptionDescription subscriptionDescription)
        {
            var topicName = _config.TopicName;

            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            if (!namespaceManager.TopicExists(topicName))
            {
                namespaceManager.CreateTopic(new TopicDescription(topicName)
                {
                    EnablePartitioning = _config.UsePartitioning
                });
            }

            if (!namespaceManager.SubscriptionExists(topicName, _config.SubscriberName))
            {
                namespaceManager.CreateSubscription(subscriptionDescription, rule);
            }
            else
            {
                var existingRules = namespaceManager.GetRules(topicName, _config.SubscriberName);

                SubscriptionClient subscriptionClient = null;
                if (!existingRules.Any())
                {
                    _log.Info($"Creating rule {rule.Name} for subscriber {_config.SubscriberName}, as it currently does not have any rules");
                    subscriptionClient = SubscriptionClient.CreateFromConnectionString(connectionString, topicName, _config.SubscriberName);
                    subscriptionClient.AddRule(rule);
                }
                if (existingRules.Count() == 1)
                {
                    var existingRule = existingRules.First();
                    var existingFilter = (SqlFilter)existingRule.Filter;

                    var versionData = existingRule.Name.Equals("$Default") ? new [] { "$Default", "0", "0", "0"} : existingRule.Name.Split('_');
                    var existingVersion = new Version(int.Parse(versionData[1]), int.Parse(versionData[2]), int.Parse(versionData[3]));

                    var newRule = (SqlFilter)rule.Filter;
                    newRule.Validate();
                    if (existingFilter.SqlExpression != newRule.SqlExpression && existingVersion < _version)
                    {
                        subscriptionClient = SubscriptionClient.CreateFromConnectionString(connectionString, topicName, _config.SubscriberName);
                        _log.Info($"Deleting rule {existingRule.Name} for subscriber {_config.SubscriberName}, as it has been superceded by a newer version");
                        subscriptionClient.RemoveRule(existingRule.Name);

                        _log.Info($"Creating rule {rule.Name} for subscriber {_config.SubscriberName}, as it is a newer version than the one currently on the subscriber");
                        subscriptionClient.AddRule(rule);
                    }
                }
            }
        }
    }
}
