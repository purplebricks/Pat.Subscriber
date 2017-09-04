using Xunit;

namespace PB.ITOps.Messaging.PatLite.UnitTests
{
    public class SubscriberHelperTests
    {
        [Fact]
        public void WhenConnectionIsValid_ThenReturnsServiceBusAddress()
        {
            var serviceBusAddress = "***REMOVED***.servicebus.windows.net/";
            var connectionString = "Endpoint=sb://***REMOVED***.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=passkeyvalue=";

            var value = connectionString.RetrieveServiceBusAddress();

            Assert.Equal(serviceBusAddress, value);
        }

        [Fact]
        public void WhenConnectionIsEmpty_ThenReturnsEmptyServiceBusAddress()
        {
            var serviceBusAddress = "";
            var connectionString = "";

            var value = connectionString.RetrieveServiceBusAddress();

            Assert.Equal(serviceBusAddress, value);
        }

        [Fact]
        public void WhenConnectionDoesNotContainEndpointInfo_ThenReturnsEmptyServiceBusAddress()
        {
            var serviceBusAddress = "";
            var connectionString = "sb://***REMOVED***.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=passkeyvalue=";
            
            var value = connectionString.RetrieveServiceBusAddress();

            Assert.Equal(serviceBusAddress, value);
        }
    }
}
