using Pat.Subscriber.MessageMapping;
using Pat.Subscriber.UnitTests.Events;
using Pat.Subscriber.UnitTests.Handlers;
using System.Linq;
using Xunit;

namespace Pat.Subscriber.UnitTests
{
    public class AllMessageTypesHandledByTests
    {
        [Fact]
        public void Event1Handler_ShouldHandle_Event1Only()
        {
            var messageTypes = AssemblyScanner.AllMessageTypesHandledBy(typeof(Event1Handler));

            Assert.Equal(typeof(Event1), messageTypes.Single());
        }


        [Fact]
        public void Event1And2Handler_ShouldHandle_Event1_And_Event2()
        {
            var messageTypes = AssemblyScanner.AllMessageTypesHandledBy(typeof(Event1And2Handler)).ToArray();

            Assert.Equal(2, messageTypes.Length);
            Assert.Contains(typeof(Event1), messageTypes);
            Assert.Contains(typeof(Event2), messageTypes);
        }

        [Fact]
        public void BaseAndDerivedEventHandler_ShouldHandle_Eventv1_And_Eventv2()
        {
            var messageTypes = AssemblyScanner.AllMessageTypesHandledBy(typeof(BaseAndDerivedEventHandler)).ToArray();

            Assert.Equal(2, messageTypes.Length);
            Assert.Contains(typeof(Eventv1), messageTypes);
            Assert.Contains(typeof(Eventv2), messageTypes);
        }

        [Fact]
        public void BaseEventHandler_ShouldHandle_Eventv1_Only()
        {
            var messageTypes = AssemblyScanner.AllMessageTypesHandledBy(typeof(BaseEventHandler)).ToArray();

            Assert.Single(messageTypes);
            Assert.Contains(typeof(Eventv1), messageTypes);
        }
    }
}