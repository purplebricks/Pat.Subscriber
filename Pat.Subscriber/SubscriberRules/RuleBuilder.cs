using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace Pat.Subscriber.SubscriberRules
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
        
        private static string GenerateSyntheticFilterClause(string handlerFullName)
        {
            var syntheticFilter = "";
            if (handlerFullName != null)
            {
                syntheticFilter = $"'{handlerFullName}.' like DomainUnderTest +'%'";
            }

            if (string.IsNullOrEmpty(syntheticFilter))
            {
                return $"(NOT EXISTS(Synthetic) OR (Synthetic <> 'true' AND Synthetic <> 'True' AND Synthetic <> 'TRUE'))";
            }

            return $"(NOT EXISTS(Synthetic) OR (Synthetic <> 'true' AND Synthetic <> 'True' AND Synthetic <> 'TRUE') OR {syntheticFilter})";
        }

        public List<string> GenerateMessageTypeFilterClause(IEnumerable<string> messagesTypeFilters, int initialFilterLength)
        {
            const int maxRuleLength = 1024;
            const int messageTypeFilterPrefixLengthOverhead = 20;

            var filterLength = initialFilterLength + messageTypeFilterPrefixLengthOverhead;

            var messageTypes = new List<string>();
            var rules = new List<string>();
            string customMessageTypeRule;

            foreach (var item in messagesTypeFilters)
            {
                if (filterLength + item.Length < maxRuleLength)
                {
                    var newItem = $"'{item}',";
                    messageTypes.Add(newItem);
                    filterLength += newItem.Length;
                }
                else
                {
                    var newItem = $"'{item}',";

                    customMessageTypeRule = messageTypes.Any() ? $"MessageType IN ({string.Join("", messageTypes.ToArray()).TrimEnd(',')})" : $"MessageType IN ('{item}')";
                    rules.Add(customMessageTypeRule);
                    messageTypes.Clear();
                    messageTypes.Add(newItem);
                    filterLength = initialFilterLength + messageTypeFilterPrefixLengthOverhead + newItem.Length;
                }
            }

            if (messageTypes.Any())
            {
                customMessageTypeRule = $"MessageType IN ({string.Join("", messageTypes.ToArray()).TrimEnd(',')})";
                rules.Add(customMessageTypeRule);
            }

            return rules;
        }

        private static string GenerateSubscriberFilterClause(string subscriberName)
        {
            return $"(NOT EXISTS(SpecificSubscriber) OR SpecificSubscriber = '{subscriberName}')";
        }

        public IEnumerable<RuleDescription> GenerateSubscriptionRules(IEnumerable<string> messagesTypeFilters, string handlerFullName, bool omitSpecificSubscriberFilter = false)
        {
            var syntheticFilter = GenerateSyntheticFilterClause(handlerFullName);

            var filterLength = 0;
            var filter = "";

            if (omitSpecificSubscriberFilter)
            {
                filter = $" AND {syntheticFilter}";
                filterLength = filter.Length;
            }
            else
            {
                var specificSubscriberOrAllRule = GenerateSubscriberFilterClause(_subscriberName);
                filter = $" AND {specificSubscriberOrAllRule} AND {syntheticFilter}";
                filterLength = filter.Length;
            }
            
            var customMessageTypes = GenerateMessageTypeFilterClause(messagesTypeFilters, filterLength);

            var rules = new List<RuleDescription>();
            var count = 1;

            foreach (var item in customMessageTypes)
            {
                rules.Add(new RuleDescription($"{count}_v_{_version.Major}_{_version.Minor}_{_version.Build}")
                {
                    Filter = new SqlFilter($"{item} {filter}")
                });
                count++;
            }

            return rules;
        }

        public async Task ApplyRuleChanges(RuleDescription[] newRules, RuleDescription[] existingRules, string[] messagesTypes)
        {
            var newRulesAlreadyPresent = new List<Rule>();

            foreach (var newRuleDescription in newRules)
            {
                var existingRulesWithSameName = new List<Rule>();

                foreach (var ruleDescription in existingRules)
                {
                    var oldRule = GetRuleVersion(ruleDescription);
                    var newRule = GetRuleVersion(newRuleDescription);

                    if (oldRule.RuleDescription.Name == newRule.RuleDescription.Name)
                    {
                        existingRulesWithSameName.Add(oldRule);
                        break;
                    }

                    if (oldRule.Name == newRule.Name && oldRule.Version >= newRule.Version)
                    {
                        existingRulesWithSameName.Add(oldRule);
                        break;
                    }
                }

                if (!existingRulesWithSameName.Any())
                {
                    continue;
                }

                newRulesAlreadyPresent.Add(GetRuleVersion(newRuleDescription));

                if (((SqlFilter)existingRulesWithSameName.First().RuleDescription.Filter).SqlExpression !=
                    ((SqlFilter)newRuleDescription.Filter).SqlExpression)
                {
                    throw new InvalidOperationException("Message types inside the assembly have changed, but the assembly version number has not.");
                }
            }

            if (newRulesAlreadyPresent.Count < newRules.Length)
            {
                foreach (var newRule in GetNewRulesNotAlreadyPresent(newRules, newRulesAlreadyPresent.Select(x => x.RuleDescription.Name)))
                {
                    await _ruleApplier.AddRule(newRule).ConfigureAwait(false);
                }
            }

            foreach (var existingRule in GetOutdatedExistingRules(existingRules, newRules))
            {
                await _ruleApplier.RemoveRule(existingRule).ConfigureAwait(false);
            }
        }

        public static Rule GetRuleVersion(RuleDescription ruleDescription)
        {
            if (ruleDescription.Name == "$Default")
            {
                return new Rule
                {
                    RuleDescription = ruleDescription,
                    Version = new Version(1, 0)
                };
            }

            var reversedRuleName = ruleDescription.Name.Split('_').Reverse().ToList();

            var rule = new Rule
            {
                RuleDescription = ruleDescription,
                Version = new Version(int.Parse(reversedRuleName[2]), int.Parse(reversedRuleName[1]),
                    int.Parse(reversedRuleName[0]))
            };

            rule.Name = ruleDescription.Name.Replace("_" + rule.Version.ToString().Replace(".", "_"), "");
            return rule;
        }

        private static IEnumerable<RuleDescription> GetNewRulesNotAlreadyPresent(
            IEnumerable<RuleDescription> newRules,
            IEnumerable<string> newRulesAlreadyPresent)
                => newRules.Where(r => !newRulesAlreadyPresent.Contains(r.Name));

        private static IEnumerable<RuleDescription> GetOutdatedExistingRules(
            IEnumerable<RuleDescription> existingRules,
            IEnumerable<RuleDescription> newRules)
            => existingRules.Where(
                r => newRules.All(newRule => r.Name != newRule.Name && GetRuleVersion(r).Version <= GetRuleVersion(newRule).Version));
    }

    public class Rule
    {
        public RuleDescription RuleDescription { get; set; }
        public string Name { get; set; }
        public Version Version { get; set; }
    }
}