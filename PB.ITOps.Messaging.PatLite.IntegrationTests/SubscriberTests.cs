using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.PatSender;
using Xunit;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests
{
    public class SubscriberTests
    {
        private static readonly IServiceProvider ServiceProvider;

        static SubscriberTests()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile(@"Configuration\appsettings.json");
            var configuration = configurationBuilder.Build();

            ServiceProvider = IoC.Initialize(configuration);

            var subscriber = ServiceProvider.GetService<Subscriber>();
            Task.Run(() => subscriber.Run());
        }

        [Fact]
        public async Task When_MessagePublished_HandlerReceivesMessageWithCorrectCorrelationId()
        {
            var messagePublisher = ServiceProvider.GetService<IMessagePublisher>();
            var correlationId = Guid.NewGuid().ToString();

            await messagePublisher.PublishEvent(new TestEvent(), new MessageProperties(correlationId));

            Wait.UntilIsNotNull(() =>
                TestEventHandler.ReceivedEvents.FirstOrDefault(m => m.CorrelationId == correlationId),
                $"'{nameof(TestEvent)}' message never received for correlation id '{correlationId}'");
        }

        [Fact]
        public async Task When_SynthenticMessagePublishedWithFullDomain_HandlerReceivesMessage()
        {
            var messagePublisher = ServiceProvider.GetService<IMessagePublisher>();
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
            var messagePublisher = ServiceProvider.GetService<IMessagePublisher>();
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
