using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PB.ITOps.Messaging.DataProtection;
using PB.ITOps.Messaging.PatSender;
using PB.ITOps.Messaging.PatSender.Correlation;
using PB.ITOps.Messaging.PatSender.Encryption;
using Xunit;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests
{
    public class SubscriberTests: IClassFixture<SubscriberFixture>
    {
        private readonly IGenericServiceProvider _serviceProvider;

        public SubscriberTests(SubscriberFixture subscriberFixture)
        {
            _serviceProvider = subscriberFixture.ServiceProvider;
        }

        private T GetService<T>()
        {
            return _serviceProvider.GetService<T>();
        }

        [Fact]
        public async Task When_EncryptedMessagePublished_HandlerReceivesDecryptedMessage()
        {
            var encryptedMessageGenerator = new EncryptedMessageGenerator(GetService<DataProtectionConfiguration>());
            var messagePublisher = new MessagePublisher(
                GetService<IMessageSender>(),
                encryptedMessageGenerator, 
                new MessageProperties(GetService<ICorrelationIdProvider>()));

            var correlationId = Guid.NewGuid().ToString();

            var testMessageToBeEncrypted = "test encryption";
            await messagePublisher.PublishEvent(new TestEvent
            {
                Data = testMessageToBeEncrypted
            }, new MessageProperties(correlationId));

            Wait.UntilIsNotNull(() =>
                    TestEventHandler.ReceivedEvents.FirstOrDefault(m => m.CorrelationId == correlationId && m.Event.Data == testMessageToBeEncrypted),
                $"'{nameof(TestEvent)}' message never received for correlation id '{correlationId}'");
        }

        [Fact]
        public async Task When_MessagePublished_HandlerReceivesMessageWithCorrectCorrelationId()
        {
            var messagePublisher = GetService<IMessagePublisher>();
            var correlationId = Guid.NewGuid().ToString();

            await messagePublisher.PublishEvent(new TestEvent(), new MessageProperties(correlationId));

            Wait.UntilIsNotNull(() =>
                TestEventHandler.ReceivedEvents.FirstOrDefault(m => m.CorrelationId == correlationId),
                $"'{nameof(TestEvent)}' message never received for correlation id '{correlationId}'");
        }

        [Fact]
        public async Task When_SynthenticMessagePublishedWithFullDomain_HandlerReceivesMessage()
        {
            var messagePublisher = GetService<IMessagePublisher>();
            var correlationId = Guid.NewGuid().ToString();

            var domainUnderTest = "PB.ITOps.Messaging.PatLite.IntegrationTests.";
            await messagePublisher.PublishEvent(new TestEvent(), new MessageProperties(correlationId)
            {
                CustomProperties = new Dictionary<string, string>
                {
                    { "DomainUnderTest", domainUnderTest },
                    { "Synthetic", "true" }
                }
            });

            Wait.UntilIsNotNull(() =>
                TestEventHandler.ReceivedEvents.FirstOrDefault(m => m.CorrelationId == correlationId),
                $"'{nameof(TestEvent)}' message never received when sythetic and domain under test is set as '{domainUnderTest}'");
        }

        [Fact]
        public async Task When_SynthenticMessagePublishedInDifferentDomain_HandlerDoesNotReceiveTheMessage()
        {
            var messagePublisher = GetService<IMessagePublisher>();
            var correlationId = Guid.NewGuid().ToString();

            var domainUnderTest = "PB.Offers.";
            await messagePublisher.PublishEvent(new TestEvent(), new MessageProperties(correlationId)
            {
                CustomProperties = new Dictionary<string, string>
                {
                    { "DomainUnderTest", domainUnderTest },
                    { "Synthetic", "true" }
                }
            });

             Wait.ToEnsureIsNull(() =>
                TestEventHandler.ReceivedEvents.FirstOrDefault(m => m.CorrelationId == correlationId),
                $"'{nameof(TestEvent)}' message incorrectly received when sythetic and domain under test is set as '{domainUnderTest}'");
        }
    }
}
