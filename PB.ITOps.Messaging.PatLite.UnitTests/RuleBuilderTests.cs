using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ServiceBus.Messaging;
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
            var messagesTypes = new[] {eventName};
            
            var rule = _ruleBuilder.GenerateSubscriptionRule(messagesTypes, _handlerName);

            var filter = ((SqlFilter) rule.Filter).SqlExpression;
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

            var rule = _ruleBuilder.GenerateSubscriptionRule(messagesTypes, _handlerName);

            var filter = ((SqlFilter)rule.Filter).SqlExpression;
            for (int i = 0; i < 10; i++)
            {
                Assert.Contains($"'{messagesTypes[i]}'", filter);
            }
        }

        [Fact]
        public void BuildRules_ExistingRuleIsUpToDate_DoesNotChangeRules()
        {
            var messagesTypes = new[] { "TestEvent" };
            var rule = _ruleBuilder.GenerateSubscriptionRule(messagesTypes, _handlerName);
            _ruleBuilder.BuildRules(rule, new [] { rule }, messagesTypes);

            _ruleApplier.DidNotReceiveWithAnyArgs().AddRule(null);
            _ruleApplier.DidNotReceiveWithAnyArgs().RemoveRule(null);
        }

        [Fact]
        public void BuildRules_NoExistingRules_CallAddRule()
        {
            var messagesTypes = new[] { "TestEvent" };
            var rule = _ruleBuilder.GenerateSubscriptionRule(messagesTypes, _handlerName);
            _ruleBuilder.BuildRules(rule, new RuleDescription [] { }, messagesTypes);

            _ruleApplier.Received(1).AddRule(rule);
            _ruleApplier.DidNotReceiveWithAnyArgs().RemoveRule(null);
        }

        [Fact]
        public void BuildRules_ExistingRuleMissingNewEvent_WhenSameVersion_ThrowsException()
        {
            var oldMessageTypes = new[] { "TestEvent" };
            var newMessageTypes = oldMessageTypes.Union(new[] {"NewEvent"}).ToArray();

            var existingRule = _ruleBuilder.GenerateSubscriptionRule(oldMessageTypes, _handlerName);
            var newRule = _ruleBuilder.GenerateSubscriptionRule(newMessageTypes, _handlerName);

            Assert.Throws<InvalidOperationException>(
                () => _ruleBuilder.BuildRules(newRule, new[] {existingRule}, newMessageTypes));
        }

        [Fact]
        public void BuildRules_ExistingRuleMissingNewEvent_WhenNewVersion_AddsNewRule()
        {
            var oldMessageTypes = new[] { "TestEvent" };
            var newMessageTypes = oldMessageTypes.Union(new[] { "NewEvent" }).ToArray();

            var oldRuleVersionResolver = Substitute.For<IRuleVersionResolver>();
            oldRuleVersionResolver.GetVersion().Returns(new Version(0, 1, 0));
            var oldRuleBuilder = new RuleBuilder(_ruleApplier, oldRuleVersionResolver, "SubscriberName");
            var existingRule = oldRuleBuilder.GenerateSubscriptionRule(oldMessageTypes, _handlerName);

            var newRule = _ruleBuilder.GenerateSubscriptionRule(newMessageTypes, _handlerName);

            _ruleBuilder.BuildRules(newRule, new[] { existingRule }, newMessageTypes);

            _ruleApplier.Received(1).AddRule(newRule);
        }

        [Fact]
        public void BuildRules_ExistingRuleMissingNewEvent_WhenNewVersion_RemovesOldRule()
        {
            var oldMessageTypes = new[] { "TestEvent" };
            var newMessageTypes = oldMessageTypes.Union(new[] { "NewEvent" }).ToArray();

            var oldRuleVersionResolver = Substitute.For<IRuleVersionResolver>();
            oldRuleVersionResolver.GetVersion().Returns(new Version(0, 1, 0));
            var oldRuleBuilder = new RuleBuilder(_ruleApplier, oldRuleVersionResolver, "SubscriberName");
            var existingRule = oldRuleBuilder.GenerateSubscriptionRule(oldMessageTypes, _handlerName);

            var newRule = _ruleBuilder.GenerateSubscriptionRule(newMessageTypes, _handlerName);

            _ruleBuilder.BuildRules(newRule, new[] { existingRule }, newMessageTypes);

            _ruleApplier.Received(1).RemoveRule(existingRule);
        }

        [Fact]
        public void GenerateSubscriptionRule_ContainsSyntheticCheck()
        {
            var eventName = "TestEvent";
            var messagesTypes = new[] { eventName };
            var rule = _ruleBuilder.GenerateSubscriptionRule(messagesTypes, _handlerName);

            var filter = ((SqlFilter)rule.Filter).SqlExpression;
            Assert.Contains("(NOT EXISTS(Synthetic) OR Synthetic <> 'true' ", filter);
        }

        [Fact]
        public void GenerateSubscriptionRule_ContainsDomainUnderTestComparisonBAseOnHandlerName()
        {
            var eventName = "TestEvent";
            var handlerName = "PB.Offer.Sales.BuyerQualification.Handler";
            var messagesTypes = new[] { eventName };
            var rule = _ruleBuilder.GenerateSubscriptionRule(messagesTypes, handlerName);

            var filter = ((SqlFilter)rule.Filter).SqlExpression;
            
            Assert.Contains($"'{handlerName}.' like DomainUnderTest +'%'", filter);
        }
    }
}
