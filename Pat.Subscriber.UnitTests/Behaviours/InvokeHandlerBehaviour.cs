using System;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using NSubstitute;
using Pat.Subscriber.Deserialiser;
using Pat.Subscriber.IoC;
using Pat.Subscriber.MessageMapping;
using Pat.Subscriber.MessageProcessing;
using Xunit;

namespace Pat.Subscriber.UnitTests.Behaviours
{
    public class InvokeHandlerBehaviourTests
    {
        public class FakeHandler : IHandleEvent<TestEvent>
        {
            public Task HandleAsync(TestEvent message)
            {
                throw new NotImplementedException();
            }
        }

        public class TestEvent
        {
        }

        private readonly ILog _log = Substitute.For<ILog>();
        private readonly SubscriberConfiguration _config = Substitute.For<SubscriberConfiguration>();

        [Fact]
        public async Task When_InvokedHandlerThrowsException_ThenBehaviourThrowsUnwrappedException()
        {
            var invokeHandlerBehaviour = Substitute.ForPartsOf<InvokeHandlerBehaviour>(_log, _config);
            invokeHandlerBehaviour.GetHandlerForMessageType(Arg.Any<Message>())
                .ReturnsForAnyArgs(new MessageTypeMapping(typeof(TestEvent), typeof(FakeHandler)));

            var messageDependencyScope = Substitute.For<IMessageDependencyScope>();
            messageDependencyScope.GetService(null).ReturnsForAnyArgs(new FakeHandler());
            messageDependencyScope.GetService<IMessageDeserialiser>().Returns(new NewtonsoftMessageDeserialiser());
            var messageContext = new MessageContext
            {
                Message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new TestEvent()))),
                DependencyScope = messageDependencyScope
            };

            await Assert.ThrowsAsync<NotImplementedException>(() =>
            {
                return invokeHandlerBehaviour.Invoke(context => Task.CompletedTask, messageContext);
            });
        }
    }
}
