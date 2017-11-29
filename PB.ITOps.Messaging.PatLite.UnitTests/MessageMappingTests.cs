using PB.ITOps.Messaging.PatLite.MessageMapping;
using Xunit;

namespace PB.ITOps.Messaging.PatLite.UnitTests
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