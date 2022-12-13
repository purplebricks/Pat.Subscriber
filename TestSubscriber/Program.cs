using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pat.Subscriber;
using Pat.Subscriber.MessageMapping;
using Pat.Subscriber.NetCoreDependencyResolution;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TestSubscriber
{
    internal class Program
    {
        private static async Task Main()
        {
            var serviceProvider = InitialiseIoC();

            var tokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, args) =>
            {
                var log = serviceProvider.GetService<ILogger<Program>>();
                log.LogInformation("Subscriber Shutdown Requested");
                args.Cancel = true;
                tokenSource.Cancel();
            };

            var subscriber = serviceProvider.GetService<Pat.Subscriber.Subscriber>();
            await subscriber.Initialise(new[] {Assembly.GetExecutingAssembly()},
                new CustomMessageTypeMap[]
                {
                    new CustomMessageTypeMap("TestSubscriber.MyEvent3", typeof(MyEvent2), typeof(PatLiteTestHandler))
                });
            
            await subscriber.ListenForMessages(tokenSource);
        }

        private static ServiceProvider InitialiseIoC()
        {
            var connection = "Endpoint=sb://namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=YOURKEY";
            var topicName = "pat";

            var subscriberConfiguration = new SubscriberConfiguration
            {
                ConnectionStrings = new[] { connection },
                TopicName = topicName,
                SubscriberName = "PatExampleSubscriber",
                UseDevelopmentTopic = true
            };

            var serviceProvider = new ServiceCollection()
                .AddPatLite(subscriberConfiguration)
                .AddLogging(b => b.AddConsole())
                .AddHandlersFromAssemblyContainingType<PatLiteTestHandler>()
                .BuildServiceProvider();

            return serviceProvider;
        }
    }
}
