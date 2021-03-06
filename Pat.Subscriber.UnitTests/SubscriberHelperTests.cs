﻿using Xunit;

namespace Pat.Subscriber.UnitTests
{
    public class SubscriberHelperTests
    {
        [Fact]
        public void WhenConnectionIsValid_ThenReturnsServiceBusAddress()
        {
            var serviceBusAddress = "namespace.servicebus.windows.net/";
            var connectionString = "Endpoint=sb://namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=passkeyvalue=";

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
            var connectionString = "sb://namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=passkeyvalue=";
            
            var value = connectionString.RetrieveServiceBusAddress();

            Assert.Equal(serviceBusAddress, value);
        }
    }
}
