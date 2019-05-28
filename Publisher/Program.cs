using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pat.Sender;
using Pat.Sender.Correlation;
using Pat.Sender.MessageGeneration;
using Pat.Sender.NetCoreLog;
using TestSubscriber;

namespace Publisher
{
    internal static class Program
    {
        private static async Task Main()
        {
            var serviceProvider = InitialiseIoC();

            var publisher = serviceProvider.GetService<IMessagePublisher>();

            
            Console.WriteLine("Publishing");
            await publisher.PublishEvent(new MyEvent3());
            Console.WriteLine("Published");
        }

        private static ServiceProvider InitialiseIoC()
        {
            var connection = "Endpoint=sb://namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=YOURKEY";
            var topicName = "pat";

            var settings = new PatSenderSettings
            {
                TopicName = topicName,
                PrimaryConnection = connection,
                UseDevelopmentTopic = true
            };

            var serviceProvider = new ServiceCollection()
                .AddLogging(b => b.AddConsole())
                .AddPatSender(settings)
                .BuildServiceProvider();

            return serviceProvider;
        }

        private static IServiceCollection AddPatSender(this IServiceCollection services, PatSenderSettings settings)
            => services
                .AddPatSenderNetCoreLogAdapter()
                .AddSingleton(settings)
                .AddTransient<IMessagePublisher, MessagePublisher>()
                .AddTransient<IMessageSender, MessageSender>()
                .AddTransient<IMessageGenerator, MessageGenerator>()
                .AddTransient(s => new MessageProperties(new LiteralCorrelationIdProvider($"{Guid.NewGuid()}")));
    }
}
