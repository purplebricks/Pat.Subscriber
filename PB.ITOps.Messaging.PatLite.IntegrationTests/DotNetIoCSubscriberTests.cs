using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PB.ITOps.Messaging.DataProtection;
using PB.ITOps.Messaging.PatLite.Deserialiser;
using PB.ITOps.Messaging.PatLite.Encryption;
using PB.ITOps.Messaging.PatLite.MonitoringPolicy;
using PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution;
using PB.ITOps.Messaging.PatSender;
using PB.ITOps.Messaging.PatSender.Encryption;
using PB.ITOps.Messaging.PatSender.MessageGeneration;
using Xunit;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests
{
    public class DotNetIocSubscriberTests
    {
        private CancellationTokenSource _cancellationTokenSource;
        private IConfigurationRoot _configuration;
        private SubscriberConfiguration _subscriberConfiguration;
        private IStatisticsReporter _statisticsReporter;

        public DotNetIocSubscriberTests()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile(@"Configuration\appsettings.json");
            _configuration = configurationBuilder.Build();

            _subscriberConfiguration = new SubscriberConfiguration();
            _configuration.GetSection("PatLite:Subscriber").Bind(_subscriberConfiguration);
        }

        public IServiceProvider InitialiseIoC(IServiceCollection serviceCollection)
        {
            var senderSettings = new PatSenderSettings();
            _configuration.GetSection("PatLite:Sender").Bind(senderSettings);

            var statisticsConfiguration = new StatisticsReporterConfiguration();
            _configuration.GetSection("StatsD").Bind(statisticsConfiguration);

            var dataProtectionConfiguration = new DataProtectionConfiguration();
            _configuration.GetSection("DataProtection").Bind(dataProtectionConfiguration);

            _statisticsReporter = Substitute.For<IStatisticsReporter>();

            InitLogger();

            var serviceProvider = serviceCollection
                .AddSingleton(senderSettings)
                .AddSingleton(statisticsConfiguration)
                .AddSingleton(dataProtectionConfiguration)
                .AddSingleton<IMessageGenerator, MessageGenerator>()
                .AddTransient<IEncryptedMessagePublisher>(
                    provider => new EncryptedMessagePublisher(
                        provider.GetRequiredService<IMessageSender>(),
                        provider.GetRequiredService<DataProtectionConfiguration>(),
                        new MessageProperties(Guid.NewGuid().ToString())))
                .AddTransient<IMessagePublisher>(
                    provider => new MessagePublisher(
                        provider.GetRequiredService<IMessageSender>(),
                        provider.GetRequiredService<IMessageGenerator>(),
                        new MessageProperties(Guid.NewGuid().ToString())))
                .AddSingleton(LogManager.GetLogger("IntegrationLogger"))
                .AddTransient<IMessageSender, MessageSender>()
                .AddSingleton(_statisticsReporter)
                .AddHandlersFromAssemblyContainingType<DotNetIoC>()
                .BuildServiceProvider();

            return serviceProvider;
        }

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

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Token.WaitHandle.WaitOne();
        }

        [Fact]
        public async Task Given_AddPatLiteExtension_When_MessagePublished_HandlerReceivesMessageWithCorrectCorrelationId()
        {
            var collection = new ServiceCollection()
                .AddSingleton(_subscriberConfiguration)
                .AddPatLite(new PatLiteOptions
                {
                    MessageDeserialiser = provider => provider.GetService<MessageContext>().MessageEncrypted
                        ? new EncryptedMessageDeserialiser(provider.GetService<DataProtectionConfiguration>())
                        : (IMessageDeserialiser) new NewtonsoftMessageDeserialiser(),
                    SubscriberConfiguration = _subscriberConfiguration
                });
            var serviceProvider = InitialiseIoC(collection);

            var subscriber = serviceProvider.GetService<Subscriber>();
            _cancellationTokenSource = new CancellationTokenSource();
            if (subscriber.Initialise(new[] { typeof(SubscriberTests).Assembly }).GetAwaiter().GetResult())
            {
                Task.Run(() => subscriber.ListenForMessages(_cancellationTokenSource));
            }

            var messagePublisher = serviceProvider.GetService<IMessagePublisher>();
            var correlationId = Guid.NewGuid().ToString();

            await messagePublisher.PublishEvent(new TestEvent(), new MessageProperties(correlationId));

            Wait.UntilIsNotNull(() =>
                TestEventHandler.ReceivedEvents.FirstOrDefault(m => m.CorrelationId == correlationId),
                $"'{nameof(TestEvent)}' message never received for correlation id '{correlationId}'");
        }

        [Fact]
        public async Task Given_AddPatLiteExtension_WhenHandlerProcessesMessage_ThenMonitoringIncrementsMessageProcessed()
        {
            var collection = new ServiceCollection()
                .AddPatLite(new PatLiteOptions
                {
                    MessageDeserialiser = provider => provider.GetService<MessageContext>().MessageEncrypted
                        ? new EncryptedMessageDeserialiser(provider.GetService<DataProtectionConfiguration>())
                        : (IMessageDeserialiser)new NewtonsoftMessageDeserialiser(),
                    SubscriberConfiguration = _subscriberConfiguration
                });
            var serviceProvider = InitialiseIoC(collection);

            var subscriber = serviceProvider.GetService<Subscriber>();
            _cancellationTokenSource = new CancellationTokenSource();
            if (subscriber.Initialise(new[] { typeof(SubscriberTests).Assembly }).GetAwaiter().GetResult())
            {
                Task.Run(() => subscriber.ListenForMessages(_cancellationTokenSource));
            }

            var messagePublisher = serviceProvider.GetService<IMessagePublisher>();
            var correlationId = Guid.NewGuid().ToString();
            
            await messagePublisher.PublishEvent(new TestEvent(), new MessageProperties(correlationId));

            Wait.UntilIsNotNull(() =>
                    TestEventHandler.ReceivedEvents.FirstOrDefault(m => m.CorrelationId == correlationId),
                $"'{nameof(TestEvent)}' message never received for correlation id '{correlationId}'");

            _statisticsReporter.Received().Increment(Arg.Is<string>(e => e.Equals("MessageProcessed", StringComparison.InvariantCultureIgnoreCase)), Arg.Any<string>());
        }
    }
}
