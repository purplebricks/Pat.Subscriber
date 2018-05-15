using System.Linq;
using System.Reflection;
using Pat.Subscriber.MessageMapping;
using Pat.Subscriber.UnitTests.Events;
using Pat.Subscriber.UnitTests.Handlers;
using Xunit;

namespace Pat.Subscriber.UnitTests
{
    public class AddDerivedTypeMappingsTests
    {
        [Fact]
        public void BaseEventHandler_AlsoHandlesDerivedMessage()
        {
            var handlerMappings = new[] { new MessageTypeMapping(typeof(Eventv1), typeof(BaseEventHandler)) };

            var allTypes = Assembly.GetExecutingAssembly().GetTypes();
            var messageTypes =
                AssemblyScanner.AddDerivedTypeMappings(handlerMappings, allTypes).Select(m => m.MessageType).ToArray();


            Assert.Equal(2, messageTypes.Length);
            Assert.Contains(typeof(Eventv1), messageTypes);
            Assert.Contains(typeof(Eventv2), messageTypes);
        }

        [Fact]
        public void When_SeparateHandlerForDerivedAndBaseMessage_NoNewMappingsAdded()
        {
            var handlerMappings = new[]
            {
                new MessageTypeMapping(typeof(Eventv1), typeof(BaseEventHandler)),
                new MessageTypeMapping(typeof(Eventv2), typeof(DerivedEventHandler)), 
            };

            var allTypes = Assembly.GetExecutingAssembly().GetTypes();
            var combinedHandlerMappings = AssemblyScanner.AddDerivedTypeMappings(handlerMappings, allTypes).ToArray();


            Assert.Equal(handlerMappings.Length, combinedHandlerMappings.Length);
            Assert.Contains(combinedHandlerMappings, mapping => handlerMappings[0].MessageType == mapping.MessageType && handlerMappings[0].HandlerType == mapping.HandlerType);
            Assert.Contains(combinedHandlerMappings, mapping => handlerMappings[1].MessageType == mapping.MessageType && handlerMappings[1].HandlerType == mapping.HandlerType);
        }
    }
}
