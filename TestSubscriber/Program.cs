using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Pat.DataProtection;
using Pat.Sender;
using Pat.Sender.Correlation;
using Pat.Sender.DataProtectionEncryption;
using Pat.Subscriber;
using Pat.Subscriber.BatchProcessing;
using Pat.Subscriber.CicuitBreaker;
using Pat.Subscriber.DataProtectionDecryption;
using Pat.Subscriber.Deserialiser;
using Pat.Subscriber.MessageProcessing;
using Pat.Subscriber.RateLimiterPolicy;
using Pat.Subscriber.StructureMap4DependencyResolution;
using Pat.Subscriber.Telemetry.StatsD;
using StructureMap;

namespace TestSubscriber
{

    internal class Program
    {
        private static void InitLogger()
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();
            var tracer = new TraceAppender();
            var patternLayout = new PatternLayout();

            patternLayout.ConversionPattern = "%d [%t] %-5p %m%n";
            patternLayout.ActivateOptions();

            tracer.Layout = patternLayout;
            tracer.ActivateOptions();
            hierarchy.Root.AddAppender(tracer);

            var roller = new RollingFileAppender();
            roller.Layout = patternLayout;
            roller.AppendToFile = true;
            roller.RollingStyle = RollingFileAppender.RollingMode.Size;
            roller.MaxSizeRollBackups = 4;
            roller.MaximumFileSize = "100KB";
            roller.StaticLogFileName = true;
            roller.File = "IntegrationLogger.txt";
            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);

            hierarchy.Root.Level = Level.All;
            hierarchy.Configured = true;
        }

        private static async Task Main()
        {
            var tokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, args) =>
            {
                Console.WriteLine("Subscriber Shutdown Requested");
                args.Cancel = true;
                tokenSource.Cancel();
            };

            var container = Initialize();

            var messagePublisher = container.GetInstance<IMessagePublisher>();

            await messagePublisher.PublishEvent(new MyEvent1()
                , new MessageProperties("")
                {
                    CustomProperties = new Dictionary<string, string>
                    {
                        { "Synthetic", "true"},
                        { "DomainUnderTest", "TestSubscriber." }
                    }
                });

            var subscriber = container.GetInstance<Subscriber>();
            await subscriber.Initialise(new[] { Assembly.GetExecutingAssembly() });
            await subscriber.ListenForMessages(tokenSource);
        }

        public static IContainer Initialize()
        {
            var connection = "Endpoint=sb://namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=YOURKEY";
            var topicName = "pat";

            var subscriberConfiguration = new SubscriberConfiguration
            {
                ConnectionStrings = new[] { connection },
                TopicName = topicName,
                UsePartitioning = true,
                SubscriberName = "PatLiteTestSubscriber",
                BatchSize = 100,
                ConcurrentBatches = 1,
                UseDevelopmentTopic = true
            };
            var patSenderConfig = new PatSenderSettings
            {
                PrimaryConnection = connection,
                TopicName = topicName,
                UseDevelopmentTopic = true
            };

            var statdConfig = new StatisticsReporterConfiguration
            {
                Environment = "local",
                StatsDHost = "StatsD Host Name",
                Tenant = "uk"
            };

            var statsReporter = new StatisticsReporter(statdConfig);
            InitLogger();

            using (statsReporter.StartTimer("IocStartup", $"Client=PatLite.{subscriberConfiguration.SubscriberName}"))
            {
                var container = new Container(x =>
                {
                    x.For<CircuitBreakerBatchProcessingBehaviour.CircuitBreakerOptions>().Use(
                        new CircuitBreakerBatchProcessingBehaviour.CircuitBreakerOptions(1000, exception => false));
                    x.AddRegistry(new PatLiteRegistryBuilder(subscriberConfiguration)
                        .DefineMessagePipeline
                            .With<RateLimiterMessageProcessingBehaviour>()
                            .With<CircuitBreakerMessageProcessingBehaviour>()
                            .With<DefaultMessageProcessingBehaviour>()
                            .With<InvokeHandlerBehaviour>()
                        .DefineBatchPipeline
                            .With<RateLimiterBatchProcessingBehaviour>()
                            .With<CircuitBreakerBatchProcessingBehaviour>()
                            .With<DefaultBatchProcessingBehaviour>()
                        .WithMessageDeserialiser(ctx => ctx.GetInstance<MessageContext>().MessageEncrypted
                            ? new EncryptedMessageDeserialiser(ctx.GetInstance<DataProtectionConfiguration>())
                            : (IMessageDeserialiser)new NewtonsoftMessageDeserialiser())
                        .Build());
                });

                container.Configure(x =>
                {
                    x.Scan(scanner =>
                    {
                        scanner.WithDefaultConventions();
                        scanner.AssemblyContainingType<IMessagePublisher>();
                    });

                    x.For<IStatisticsReporter>().Use(statsReporter);

                    x.For<RateLimiterPolicyOptions>().Use(
                        new RateLimiterPolicyOptions(
                            new RateLimiterConfiguration
                            {
                                RateLimit = 100
                            })
                    );

                    x.For<DataProtectionConfiguration>().Use(new DataProtectionConfiguration
                    {
                        AccountName = "Blob Storage Account",
                        AccountKey = "Blob Storage Account Key",
                        Thumbprint = "CERTIFICATE THUMBPRINT"
                    });
                    x.For<ICorrelationIdProvider>().Use(new LiteralCorrelationIdProvider(""));
                    x.For<IEncryptedMessagePublisher>().Use<EncryptedMessagePublisher>()
                        .Ctor<string>().Is(ctx => ctx.GetInstance<MessageContext>().CorrelationId);
                    x.For<PatSenderSettings>().Use(patSenderConfig);
                });

                return container;
            }
        }
    }
}
