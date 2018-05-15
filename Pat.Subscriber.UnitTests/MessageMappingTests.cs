using Pat.Subscriber.MessageMapping;
using Xunit;

namespace Pat.Subscriber.UnitTests
{
    public class MessageMappingTests
    {
        [Fact]
        public void UnmappedMessageType_ShouldThrowDetailedException()
        {
            MessageMapper.MapMessageTypesToHandlers(new []{typeof(TestSubscriber.PatLiteTestHandler).Assembly});

            Assert.Throws<UnmappedMessageTypeException>(() =>
                MessageMapper.GetHandlerForMessageType("Invalid.Type, AssemblyNameHere"));
        }
    }
}