using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PB.ITOps.Messaging.DataProtection;
using PB.ITOps.Messaging.PatLite.IntegrationTests.DependencyResolution;
using PB.ITOps.Messaging.PatLite.IntegrationTests.Helpers;
using PB.ITOps.Messaging.PatSender;
using PB.ITOps.Messaging.PatSender.Encryption;
using Xunit;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests
{
    public class SubscriberTests: IClassFixture<SubscriberFixture>
    {
        private readonly IGenericServiceProvider _serviceProvider;
        private readonly bool _integrationTest;

        public SubscriberTests(SubscriberFixture subscriberFixture)
        {
            _serviceProvider = subscriberFixture.ServiceProvider;
            _integrationTest = subscriberFixture.IntegrationTest;
        }

        private T GetService<T>()
        {
            return _serviceProvider.GetService<T>();
        }

        [Fact]
        public async Task When_EncryptedMessagePublished_HandlerReceivesDecryptedMessagea()
        {
            var correlationId = Guid.NewGuid().ToString();
            var testMessageToBeEncrypted = "test encryption";
            var testMessage = new TestEvent
            {
                Data = testMessageToBeEncrypted
            };
            var messageSender = GetService<TestMessageSender>();

            var messageWaiter = await messageSender.PublishMessage(testMessage, correlationId,
                messageGenerator: new EncryptedMessageGenerator(GetService<DataProtectionConfiguration>()));

            Assert.True(messageWaiter.WaitOne() != null, $"'{nameof(TestEvent)}' message never received for correlation id '{correlationId}'");
        }

        [Fact]
        public async Task When_MessagePublished_HandlerReceivesMessageWithCorrectCorrelationId()
        {
            var correlationId = Guid.NewGuid().ToString();
            var messageSender = GetService<TestMessageSender>();

            var messageWaiter = await messageSender.PublishMessage(new TestEvent(), correlationId);

            Assert.True(messageWaiter.WaitOne() != null, $"'{nameof(TestEvent)}' message never received for correlation id '{correlationId}'");
        }

        [SkippableFact]
        public async Task When_SynthenticMessagePublishedWithFullDomain_HandlerReceivesMessage()
        {
            Skip.IfNot(_integrationTest);
            var correlationId = Guid.NewGuid().ToString();
            var domainUnderTest = "PB.ITOps.Messaging.PatLite.IntegrationTests.";

            var messageSender = GetService<TestMessageSender>();
            var messageWaiter = await messageSender.PublishMessage(new TestEvent(), new MessageProperties(correlationId)
            {
                CustomProperties = new Dictionary<string, string>
                {
                    { "DomainUnderTest", domainUnderTest },
                    { "Synthetic", "true" }
                }
            });

            Assert.True(messageWaiter.WaitOne() != null, $"'{nameof(TestEvent)}' message never received when sythetic and domain under test is set as '{domainUnderTest}'");
        }

        [SkippableFact]
        public async Task When_SynthenticMessagePublishedInDifferentDomain_HandlerDoesNotReceiveTheMessage()
        {
            Skip.IfNot(_integrationTest);
            var correlationId = Guid.NewGuid().ToString();
            var messageSender = GetService<TestMessageSender>();

            var domainUnderTest = "PB.Offers.";
            var messageWaiter = await messageSender.PublishMessage(new TestEvent(), new MessageProperties(correlationId)
            {
                CustomProperties = new Dictionary<string, string>
                {
                    { "DomainUnderTest", domainUnderTest },
                    { "Synthetic", "true" }
                }
            });

            Assert.True(messageWaiter.WaitOne(10000) == null, $"'{nameof(TestEvent)}' message incorrectly received when sythetic and domain under test is set as '{domainUnderTest}'");
        }
    }
}
