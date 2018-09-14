using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pat.DataProtection;
using Pat.Sender;
using Pat.Sender.DataProtectionEncryption;
using Pat.Subscriber.IntegrationTests.DependencyResolution;
using Pat.Subscriber.IntegrationTests.Helpers;
using Xunit;

namespace Pat.Subscriber.IntegrationTests
{
    public class SubscriberTests: IClassFixture<SubscriberFixture>
    {
        private readonly IGenericServiceProvider _serviceProvider;
        private readonly bool _integrationTest;
        private readonly bool _appVeyorCIBuild;

        public SubscriberTests(SubscriberFixture subscriberFixture)
        {
            _serviceProvider = subscriberFixture.ServiceProvider;
            _integrationTest = subscriberFixture.IntegrationTest;
            _appVeyorCIBuild = subscriberFixture.AppVeyorCIBuild;
        }

        private T GetService<T>()
        {
            return _serviceProvider.GetService<T>();
        }

        [SkippableFact]
        public async Task When_EncryptedMessagePublished_HandlerReceivesDecryptedMessagea()
        {
            Skip.IfNot(_integrationTest);
            Skip.If(_appVeyorCIBuild, "integration test not run on appveyor build agent");

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

        [SkippableFact]
        public async Task When_MessagePublished_HandlerReceivesMessageWithCorrectCorrelationId()
        {
            Skip.IfNot(_integrationTest);
            Skip.If(_appVeyorCIBuild, "integration test not run on appveyor build agent");

            var correlationId = Guid.NewGuid().ToString();
            var messageSender = GetService<TestMessageSender>();

            var messageWaiter = await messageSender.PublishMessage(new TestEvent(), correlationId);

            Assert.True(messageWaiter.WaitOne() != null, $"'{nameof(TestEvent)}' message never received for correlation id '{correlationId}'");
        }

        [SkippableFact]
        public async Task When_SynthenticMessagePublishedWithFullDomain_HandlerReceivesMessage()
        {
            Skip.IfNot(_integrationTest);
            Skip.If(_appVeyorCIBuild, "integration test not run on appveyor build agent");

            var correlationId = Guid.NewGuid().ToString();
            var domainUnderTest = "Pat.Subscriber.IntegrationTests.";

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
            Skip.If(_appVeyorCIBuild, "integration test not run on appveyor build agent");

            var correlationId = Guid.NewGuid().ToString();
            var messageSender = GetService<TestMessageSender>();

            var domainUnderTest = "Pat.Offers.";
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
