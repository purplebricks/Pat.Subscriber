using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using NSubstitute;
using PB.ITOps.Messaging.PatLite.SubscriberRules;
using Xunit;

namespace PB.ITOps.Messaging.PatLite.UnitTests
{
    public class RuleBuilderTests
    {
        private readonly RuleBuilder _ruleBuilder;
        private readonly IRuleApplier _ruleApplier;
        private readonly string _handlerName = "PB.Domain.SubDomain.Handler";

        public RuleBuilderTests()
        {
            _ruleApplier = Substitute.For<IRuleApplier>();
            var versionResolver = Substitute.For<IRuleVersionResolver>();
            versionResolver.GetVersion().Returns(new Version(1, 0, 0));
            _ruleBuilder = new RuleBuilder(_ruleApplier, versionResolver, "SubscriberName");
        }
        [Fact]
        public void GenerateSubscriptionRule_ForSingleEvent()
        {
            var eventName = "TestEvent";
            var messagesTypes = new[] { eventName };

            var rules = _ruleBuilder.GenerateSubscriptionRules(messagesTypes, _handlerName);

            var filter = ((SqlFilter)rules.First().Filter).SqlExpression;
            Assert.Contains($"'{eventName}'", filter);
        }

        [Fact]
        public void GenerateSubscriptionRule_ForMultipleEvents()
        {
            var n = 10;
            var messagesTypes = new List<string>(n);
            for (int i = 0; i < n; i++)
            {
                messagesTypes.Add($"TestEvent{i}");
            }

            var rules = _ruleBuilder.GenerateSubscriptionRules(messagesTypes, _handlerName);

            var filter = ((SqlFilter)rules.First().Filter).SqlExpression;
            for (int i = 0; i < 10; i++)
            {
                Assert.Contains($"'{messagesTypes[i]}'", filter);
            }
        }

        [Fact]
        public async Task ApplyRuleChanges_ExistingRulesAreUpToDate_DoesNotChangeRules()
        {
            var messagesTypes = new[] { "TestEvent" };
            var rules = _ruleBuilder.GenerateSubscriptionRules(messagesTypes, _handlerName).ToArray();
            await _ruleBuilder.ApplyRuleChanges(rules, rules, messagesTypes);

            await _ruleApplier.DidNotReceiveWithAnyArgs().AddRule(null);
            await _ruleApplier.DidNotReceiveWithAnyArgs().RemoveRule(null);
        }

        [Fact]
        public async Task ApplyRuleChanges_NoExistingRules_CallAddRule()
        {
            var messagesTypes = new[] { "TestEvent" };
            var rules = _ruleBuilder.GenerateSubscriptionRules(messagesTypes, _handlerName).ToArray();

            await _ruleBuilder.ApplyRuleChanges(rules, new RuleDescription[] { }, messagesTypes);
            await _ruleApplier.Received(1).AddRule(rules.First());
            await _ruleApplier.DidNotReceiveWithAnyArgs().RemoveRule(null);
        }

        [Fact]
        public void GenerateSubscriptionRule_ContainsSyntheticCheck()
        {
            var messagesTypes = CreateEnoughMessageTypesToSpanMultipleRules();
            var rules = _ruleBuilder.GenerateSubscriptionRules(messagesTypes, _handlerName);

            foreach (var rule in rules)
            {
                var filter = ((SqlFilter)rule.Filter).SqlExpression;
                Assert.Contains("(NOT EXISTS(Synthetic) OR Synthetic <> 'true' ", filter);
            }
        }

        [Fact]
        public void GenerateSubscriptionRule_ContainsDomainUnderTestComparisonBaseOnHandlerName()
        {
            var handlerName = "PB.Offer.Sales.BuyerQualification.Handler";
            var messagesTypes = CreateEnoughMessageTypesToSpanMultipleRules();
            var rules = _ruleBuilder.GenerateSubscriptionRules(messagesTypes, handlerName);

            foreach (var rule in rules)
            {
                var filter = ((SqlFilter)rule.Filter).SqlExpression;
                Assert.Contains($"'{handlerName}.' like DomainUnderTest +'%'", filter);
            }
        }

        [Fact]
        public void GeneratesMultipleSubscriptionRules_WhenMaximumRuleLengthExceeded()
        {
            var rules = _ruleBuilder.GenerateSubscriptionRules(CreateEnoughMessageTypesToSpanMultipleRules(), _handlerName);
            Assert.Equal(3, rules.Count());
        }

        private List<string> CreateEnoughMessageTypesToSpanMultipleRules()
        {
            var countOfMessageTypes = 40;
            var messageTypes = new List<string>(countOfMessageTypes);
            for (var i = 0; i < countOfMessageTypes; i++)
            {
                messageTypes.Add($"TestNamespace.TestRuleName.TestEvent{i}");
            }

            return messageTypes;
        }

        private async Task WhenAssemblyVersionChangesOnMultipleRules()
        {
            var oldMessageTypes = CreateEnoughMessageTypesToSpanMultipleRules();
            var newMessageTypes = oldMessageTypes.ToArray().Union(new[] { "NewEvent" }).ToArray();

            var oldRuleVersionResolver = Substitute.For<IRuleVersionResolver>();
            oldRuleVersionResolver.GetVersion().Returns(new Version(0, 1, 0));

            var oldRuleBuilder = new RuleBuilder(_ruleApplier, oldRuleVersionResolver, "SubscriberName");
            var existingRules = oldRuleBuilder.GenerateSubscriptionRules(oldMessageTypes, _handlerName).ToArray();

            var newRules = _ruleBuilder.GenerateSubscriptionRules(newMessageTypes, _handlerName).ToArray();

            await _ruleBuilder.ApplyRuleChanges(newRules, existingRules, newMessageTypes);
        }

        [Fact]
        public async Task WhenAssemblyVersionChangesOnMultipleRules_NewRulesAdded()
        {
            await WhenAssemblyVersionChangesOnMultipleRules();
            await _ruleApplier.Received(1).AddRule(Arg.Is<RuleDescription>(rd => rd.Name == "1_v_1_0_0"));
            await _ruleApplier.Received(1).AddRule(Arg.Is<RuleDescription>(rd => rd.Name == "2_v_1_0_0"));
            await _ruleApplier.Received(1).AddRule(Arg.Is<RuleDescription>(rd => rd.Name == "3_v_1_0_0"));
        }

        [Fact]
        public async Task WhenAssemblyVersionChangesOnMultipleRules_OldRulesRemoved()
        {
            await WhenAssemblyVersionChangesOnMultipleRules();
            await _ruleApplier.Received(1).RemoveRule(Arg.Is<RuleDescription>(rd => rd.Name == "1_v_0_1_0"));
            await _ruleApplier.Received(1).RemoveRule(Arg.Is<RuleDescription>(rd => rd.Name == "2_v_0_1_0"));
            await _ruleApplier.Received(1).RemoveRule(Arg.Is<RuleDescription>(rd => rd.Name == "3_v_0_1_0"));
        }

        [Fact]
        public async Task WhenAssemblyVersionUnchanged_AndANewMessageTypeAdded_ExceptionIsThrown()
        {
            var expectedMessageTypes = CreateEnoughMessageTypesToSpanMultipleRules();

            var existingRules = _ruleBuilder.GenerateSubscriptionRules(expectedMessageTypes.Skip(1), _handlerName).ToArray();
            var newRules = _ruleBuilder.GenerateSubscriptionRules(expectedMessageTypes, _handlerName).ToArray();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _ruleBuilder.ApplyRuleChanges(newRules, existingRules, expectedMessageTypes.ToArray()));

            await _ruleApplier.DidNotReceiveWithAnyArgs().AddRule(Arg.Any<RuleDescription>());
            await _ruleApplier.DidNotReceiveWithAnyArgs().RemoveRule(Arg.Any<RuleDescription>());
        }

        [Fact]
        public async Task WhenAssemblyVersionUnchanged_ButARuleFailedToDeployOnPreviousAttempt_MissingRuleIsAdded()
        {
            var expectedMessageTypes = CreateEnoughMessageTypesToSpanMultipleRules();

            var existingRules = _ruleBuilder.GenerateSubscriptionRules(expectedMessageTypes, _handlerName).Take(2).ToArray();
            var newRules = _ruleBuilder.GenerateSubscriptionRules(expectedMessageTypes, _handlerName).ToArray();

            await _ruleBuilder.ApplyRuleChanges(newRules, existingRules, expectedMessageTypes.ToArray());

            await _ruleApplier.Received(1).AddRule(Arg.Is<RuleDescription>(rd => rd.Name == "3_v_1_0_0"));
        }

        [Fact]
        public async Task WhenCurrentAssemblyVersionIsLowerThanExistingRulesVersion_NoRuleChangesAreMade()
        {
            var oldMessageTypes = new[] { "NewEvent" };
            var newMessageTypes = new[] { "NewEvent" };

            var oldRuleVersionResolver = Substitute.For<IRuleVersionResolver>();
            oldRuleVersionResolver.GetVersion().Returns(new Version(2, 0, 0));

            var oldRuleBuilder = new RuleBuilder(_ruleApplier, oldRuleVersionResolver, "SubscriberName");
            var existingRules = oldRuleBuilder.GenerateSubscriptionRules(oldMessageTypes, _handlerName).ToArray();

            var newRules = _ruleBuilder.GenerateSubscriptionRules(newMessageTypes, _handlerName).ToArray();
            await _ruleBuilder.ApplyRuleChanges(newRules, existingRules, newMessageTypes);

            await _ruleApplier.DidNotReceiveWithAnyArgs().AddRule(Arg.Any<RuleDescription>());
            await _ruleApplier.DidNotReceiveWithAnyArgs().RemoveRule(Arg.Any<RuleDescription>());
        }

        [Fact]
        public async Task WhenRuleNameIsInOldFormatAndIsOlderVersion_NewRuleAddedAndOldRuleRemoved()
        {
            var oldMessageTypes = new[] { "NewEvent" };
            var newMessageTypes = new[] { "NewEvent" };

            var oldRuleVersionResolver = Substitute.For<IRuleVersionResolver>();
            oldRuleVersionResolver.GetVersion().Returns(new Version(0, 1, 0));

            var oldRuleBuilder = new RuleBuilder(_ruleApplier, oldRuleVersionResolver, "SubscriberName");
            var existingRules = oldRuleBuilder.GenerateSubscriptionRules(oldMessageTypes, _handlerName).ToArray();

            existingRules.First().Name = "PB.Viewing.OpenHome.Notification.Subscriber_0_1_0";

            var newRules = _ruleBuilder.GenerateSubscriptionRules(newMessageTypes, _handlerName).ToArray();
            await _ruleBuilder.ApplyRuleChanges(newRules, existingRules, newMessageTypes);

            await _ruleApplier.Received(1).AddRule(Arg.Is<RuleDescription>(rd => rd.Name == "1_v_1_0_0"));
            await _ruleApplier.Received(1).RemoveRule(Arg.Is<RuleDescription>(rd => rd.Name == "PB.Viewing.OpenHome.Notification.Subscriber_0_1_0"));
        }

        [Fact]
        public void WhenRuleIsDefault_GetRuleVersionShouldReturnValidVersion()
        {
            var rule = new RuleDescription
            {
                Name = "$Default",
                Filter = new SqlFilter("1=0")
            };

            var ruleVersion = RuleBuilder.GetRuleVersion(rule);
            Assert.Equal(new Version(1, 0), ruleVersion.Version);
        }
    }
}