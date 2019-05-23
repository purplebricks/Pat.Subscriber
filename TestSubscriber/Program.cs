using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pat.Subscriber;
using Pat.Subscriber.NetCoreDependencyResolution;
using Pat.Subscriber.Telemetry.StatsD;

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
            await subscriber.Initialise(new[] { Assembly.GetExecutingAssembly() }, 
                new List<CustomMessageMap>{ 
                    new CustomMessageMap
                    {
                        CustomMessageType = "TestSubscriber.MyEvent3",
                        OriginalMessageType = typeof(MyEvent2),
                        HandlerType = typeof(PatLiteTestHandler)
                    }});
            await subscriber.ListenForMessages(tokenSource);
        }

        private static ServiceProvider InitialiseIoC()
        {
            var connection = "";
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
                .AddTransient<IStatisticsReporter, StatisticsReporter>()
                .AddSingleton(new StatisticsReporterConfiguration())
                .AddHandlersFromAssemblyContainingType<PatLiteTestHandler>()
                .BuildServiceProvider();

            return serviceProvider;
        }
    }
}
