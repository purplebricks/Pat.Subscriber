using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ServiceBus.Messaging;

namespace PB.ITOps.Messaging.PatLite.SubscriberRules
{
    public class RuleBuilder
    {
        private readonly Version _version;
        private readonly IRuleApplier _ruleApplier;
        private readonly string _subscriberName;

        public RuleBuilder(IRuleApplier ruleApplier, IRuleVersionResolver ruleVersionResolver, string subscriberName)
        {
            _ruleApplier = ruleApplier;
            _subscriberName = subscriberName;
            _version = ruleVersionResolver.GetVersion();
        }

        public RuleDescription GenerateSubscriptionRule(IEnumerable<string> messagesTypeFilters)
        {
            var specificSubscriberOrAllRule = $"(NOT EXISTS(SpecificSubscriber) OR SpecificSubscriber = '{_subscriberName}')";
            var customMessageTypeRule = $"MessageType IN ('{string.Join("','", messagesTypeFilters)}')";
            var combinedRules = $"({customMessageTypeRule}) AND {specificSubscriberOrAllRule}";

            var rule = new RuleDescription($"{_subscriberName}_{_version.Major}_{_version.Minor}_{_version.Build}")
            {
                Filter = new SqlFilter(combinedRules)
            };

            return rule;
        }

        private void ValidateRules(string[] messagesTypes, RuleDescription[] existingRules)
        {
            foreach (var messagesTypeFilter in messagesTypes)
            {
                if (!existingRules.Any(r =>
                {
                    var filter = ((SqlFilter)r.Filter).SqlExpression;
                    return filter.Contains($"'{messagesTypeFilter}'");
                }))
                {
                    throw new InvalidOperationException($"subscriber {_subscriberName} does not have a filter for message type '{messagesTypeFilter}'");
                }
            }
        }

        public void BuildRules(RuleDescription newRule, RuleDescription[] existingRules, string[] messagesTypes)
        {
            if (!existingRules.Any())
            {
                _ruleApplier.AddRule(newRule);
            }
            else
            {
                var newVersion = new Version(_version.Major, _version.Minor, _version.Build);

                var oldRulesToRemove = existingRules.Where(r =>
                {
                    var filterIsDifferent = ((SqlFilter) r.Filter).SqlExpression != ((SqlFilter) newRule.Filter).SqlExpression;
                    var versionData = r.Name.Equals("$Default") ? new[] {"$Default", "0", "0", "0"} : r.Name.Split('_');
                    var existingVersion = new Version(int.Parse(versionData[1]), int.Parse(versionData[2]),
                        int.Parse(versionData[3]));
                    var isOldVersion = existingVersion < newVersion;
                    return filterIsDifferent && isOldVersion;
                }).ToArray();

                var rulesToRemain = existingRules.Except(oldRulesToRemove).ToArray();
                if (rulesToRemain.Any())
                {
                    ValidateRules(messagesTypes, rulesToRemain);
                }

                if (oldRulesToRemove.Any())
                {
                    if (!rulesToRemain.Any())
                    {
                        _ruleApplier.AddRule(newRule);
                    }

                    foreach (var oldRule in oldRulesToRemove)
                    {
                        _ruleApplier.RemoveRule(oldRule);
                    }
                }
            }
        }
    }
}