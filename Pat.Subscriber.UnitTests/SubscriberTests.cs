using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Pat.Subscriber.MessageMapping;
using Xunit;

namespace Pat.Subscriber.UnitTests
{
    public class SubscriberTests
    {
        private readonly SubscriberConfiguration _subscriberConfiguration;
        private readonly IMultipleBatchProcessor _multipleBatchProcessor;
        private readonly IMessageReceiverFactory _messageReceiverFactory;
        private readonly ISubscriptionBuilder _subscriptionBuilder;
        private readonly ILogger<Subscriber> _logger;
        private readonly Subscriber _subscriber;

        public SubscriberTests()
        {
            _subscriberConfiguration = Substitute.For<SubscriberConfiguration>();
            _multipleBatchProcessor = Substitute.For<IMultipleBatchProcessor>();
            _messageReceiverFactory = Substitute.For<IMessageReceiverFactory>();
            _subscriptionBuilder = Substitute.For<ISubscriptionBuilder>();
            _logger = Substitute.For<ILogger<Subscriber>>();
            _subscriber = new Subscriber(_logger, _subscriberConfiguration, _multipleBatchProcessor, _messageReceiverFactory, _subscriptionBuilder);
        }

        [Fact]
        public async void Initialise_Throws_ArgumentException_When_HandlerAssemblies_Is_Null()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _subscriber.Initialise(null));
        }
        
        [Fact]
        public async void Initialise_Throws_ArgumentException_When_HandlerAssemblies_Is_Empty()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _subscriber.Initialise(new Assembly[0]));
        }

        [Fact]
        public async void Intiialise_Sends_HandlerAssemblies_To_SubscriptionBuilder()
        {
            var assemblies = new[] {Assembly.GetCallingAssembly()};

            await _subscriber.Initialise(assemblies);

            _subscriptionBuilder.Received(1).WithRuleVersionResolver(assemblies);
        }
        
        [Fact]
        public async void Intiialise_Sends_CustomMessageTypes_To_SubscriptionBuilder()
        {
            var customMessageTypeMap =
                new CustomMessageTypeMap("TestType", typeof(FakeMessage), typeof(FakeHandler));
            
            var customMessageTypeMaps = new[] { customMessageTypeMap };
            var assemblies = new[] {Assembly.GetCallingAssembly() };

            await _subscriber.Initialise(assemblies, customMessageTypeMaps);
            
            await _subscriptionBuilder.Received(1).Build(Arg.Is<string[]>(s => s.Single() == customMessageTypeMap.MessageType), Arg.Any<string>());
        }
        
        [Fact]
        public async void Intiialise_Configures_CustomMessageTypes_In_MessageMapper()
        {
            var customMessageTypeMap =
                new CustomMessageTypeMap("TestType", typeof(FakeMessage), typeof(FakeHandler));
            
            var customMessageTypeMaps = new[] { customMessageTypeMap };
            var assemblies = new[] {Assembly.GetCallingAssembly() };
            

            await _subscriber.Initialise(assemblies, customMessageTypeMaps);
            var actual = MessageMapper.GetHandlerForMessageType(customMessageTypeMap.MessageType);
            
            Assert.Equal(customMessageTypeMap.MappedMessageType, actual.MessageType);
            Assert.Equal(customMessageTypeMap.HandlerType, actual.HandlerType);
        }
    }

    public class FakeMessage
    {
    }

    public class FakeHandler
    {
    }
}