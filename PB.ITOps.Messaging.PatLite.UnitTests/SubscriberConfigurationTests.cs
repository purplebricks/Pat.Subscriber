using System;
using Xunit;

namespace PB.ITOps.Messaging.PatLite.UnitTests
{
    public class SubscriberConfigurationTests
    {
        [Fact]
        public void SubscriptionConfiguration_DoesNotAppendMachineName_WhenNotInDevelopmentMode()
        {
            var configuration = new SubscriberConfiguration
            {
                TopicName = "test",
                UseDevelopmentTopic = false
            };

            Assert.Equal("test", configuration.EffectiveTopicName);
        }

        [Fact]
        public void SubscriptionConfiguration_DoesAppendMachineName_WhenInDevelopmentMode()
        {
            var configuration = new SubscriberConfiguration
            {
                TopicName = "test",
                UseDevelopmentTopic = true
            };

            Assert.Equal("test" + Environment.MachineName, configuration.EffectiveTopicName);
        }

        [Fact]
        public void SubscriptionConfiguration_EntersDevelopmentModeByDefault()
        {
            var configuration = new SubscriberConfiguration();
            Assert.True(configuration.UseDevelopmentTopic);
        }
    }
}
