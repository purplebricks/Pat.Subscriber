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
using NSubstitute;
using PB.ITOps.Messaging.DataProtection;
using PB.ITOps.Messaging.PatLite.BatchProcessing;
using PB.ITOps.Messaging.PatLite.Deserialiser;
using PB.ITOps.Messaging.PatLite.Encryption;
using PB.ITOps.Messaging.PatLite.MessageProcessing;
using PB.ITOps.Messaging.PatLite.MonitoringPolicy;
using PB.ITOps.Messaging.PatLite.StructureMap4;
using PB.ITOps.Messaging.PatSender;
using PB.ITOps.Messaging.PatSender.Correlation;
using StructureMap;
using Xunit;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests
{
    public class StructureMapSubscriberTests
    {
        private readonly IConfigurationRoot _configuration;
        private readonly SubscriberConfiguration _subscriberConfiguration;
        private IStatisticsReporter _statisticsReporter;

        public StructureMapSubscriberTests()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile(@"Configuration\appsettings.json");
            _configuration = configurationBuilder.Build();

            _subscriberConfiguration = new SubscriberConfiguration();
            _configuration.GetSection("PatLite:Subscriber").Bind(_subscriberConfiguration);
            _subscriberConfiguration.SubscriberName = $"{_subscriberConfiguration.SubscriberName}-StructureMap";
        }

        public IContainer InitialiseIoC(Container container)
        {
            var senderSettings = new PatSenderSettings();
            _configuration.GetSection("PatLite:Sender").Bind(senderSettings);

            var statisticsConfiguration = new StatisticsReporterConfiguration();
            _configuration.GetSection("StatsD").Bind(statisticsConfiguration);

            var dataProtectionConfiguration = new DataProtectionConfiguration();
            _configuration.GetSection("DataProtection").Bind(dataProtectionConfiguration);

            _statisticsReporter = Substitute.For<IStatisticsReporter>();

            InitLogger();

            container.Configure(x =>
            {
                x.Scan(scanner =>
                {
                    scanner.WithDefaultConventions();
                    scanner.AssemblyContainingType<IMessagePublisher>();
                });

                x.For<IStatisticsReporter>().Use(_statisticsReporter);
                x.For<ICorrelationIdProvider>().Use(new LiteralCorrelationIdProvider(Guid.NewGuid().ToString()));
                x.For<PatSenderSettings>().Use(senderSettings);
                x.For<CapturedEvents>().Use(new CapturedEvents());
                x.For<DataProtectionConfiguration>().Use(dataProtectionConfiguration);
            });

            return container;
        }

        private static void InitLogger()
        {
            var hierarchy = (Hierarchy) LogManager.GetRepository();
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

        [Fact]
        public async Task Given_DefaultPatLiteRegistryBuilder_When_MessagePublished_HandlerReceivesMessageWithCorrectCorrelationId()
        {
            var container = new Container(x =>
            {
                x.AddRegistry(new PatLiteRegistryBuilder(_subscriberConfiguration).Build());
            });
            InitialiseIoC(container);

            var subscriber = container.GetInstance<Subscriber>();
            var cancellationTokenSource = new CancellationTokenSource();

            await subscriber.Initialise(new[] {typeof(SubscriberTests).Assembly});

            var subscriberListeningTask = Task.Run(() => subscriber.ListenForMessages(cancellationTokenSource));

            var messagePublisher = container.GetInstance<IMessagePublisher>();
            var correlationId = Guid.NewGuid().ToString();

            await messagePublisher.PublishEvent(new TestEvent(), new MessageProperties(correlationId));

            Wait.UntilIsNotNull(() =>
                    container.GetInstance<CapturedEvents>().ReceivedEvents
                        .FirstOrDefault(m => m.CorrelationId == correlationId),
                $"'{nameof(TestEvent)}' message never received for correlation id '{correlationId}'");

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Token.WaitHandle.WaitOne();

            await subscriberListeningTask;
        }

        [Fact]
        public async Task Given_DefaultPatLiteRegistryBuilder_WhenHandlerProcessesMessage_ThenMonitoringIncrementsMessageProcessed()
        {
            var container = new Container(x =>
            {
                x.AddRegistry(new PatLiteRegistryBuilder(_subscriberConfiguration)
                    .WithMessageDeserialiser(ctx => ctx.GetInstance<MessageContext>().MessageEncrypted
                    ? new EncryptedMessageDeserialiser(ctx.GetInstance<DataProtectionConfiguration>())
                    : (IMessageDeserialiser)new NewtonsoftMessageDeserialiser())
                    .Build());
            });
            InitialiseIoC(container);

            var subscriber = container.GetInstance<Subscriber>();
            var cancellationTokenSource = new CancellationTokenSource();

            await subscriber.Initialise(new[] {typeof(SubscriberTests).Assembly});

            var subscriberListeningTask = Task.Run(() => subscriber.ListenForMessages(cancellationTokenSource));

            var messagePublisher = container.GetInstance<IMessagePublisher>();
            var correlationId = Guid.NewGuid().ToString();

            await messagePublisher.PublishEvent(new TestEvent(), new MessageProperties(correlationId));

            Wait.UntilIsNotNull(() =>
                    container.GetInstance<CapturedEvents>().ReceivedEvents
                        .FirstOrDefault(m => m.CorrelationId == correlationId),
                $"'{nameof(TestEvent)}' message never received for correlation id '{correlationId}'");

            _statisticsReporter.Received()
                .Increment(
                    Arg.Is<string>(e => e.Equals("MessageProcessed", StringComparison.InvariantCultureIgnoreCase)),
                    Arg.Any<string>());

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Token.WaitHandle.WaitOne();

            await subscriberListeningTask;
        }

        [Fact]
        public async Task
            Given_A_CustomMessageProcessingStepIsAdded_WhenMessageProcessingIsInvoked_ThenTheCustomStepIsCalled()
        {
            var container = new Container(x =>
            {
                x.AddRegistry(new PatLiteRegistryBuilder(_subscriberConfiguration)
                    .WithMessageDeserialiser(ctx => ctx.GetInstance<MessageContext>().MessageEncrypted
                        ? new EncryptedMessageDeserialiser(ctx.GetInstance<DataProtectionConfiguration>())
                        : (IMessageDeserialiser)new NewtonsoftMessageDeserialiser())
                    .DefineMessagePipeline
                        .With<DefaultMessageProcessingBehaviour>()
                        .With<MockMessageProcessingBehaviour>()
                        .With<InvokeHandlerBehaviour>()
                    .Build());
            });
            InitialiseIoC(container);

            var subscriber = container.GetInstance<Subscriber>();
            var cancellationTokenSource = new CancellationTokenSource();

            await subscriber.Initialise(new[] {typeof(SubscriberTests).Assembly});

            var subscriberListeningTask = Task.Run(() => subscriber.ListenForMessages(cancellationTokenSource));

            var messagePublisher = container.GetInstance<IMessagePublisher>();
            var correlationId = Guid.NewGuid().ToString();

            await messagePublisher.PublishEvent(new TestEvent(), new MessageProperties(correlationId));

            Wait.UntilIsNotNull(() =>
                    container.GetInstance<CapturedEvents>().ReceivedEvents
                        .FirstOrDefault(m => m.CorrelationId == correlationId),
                $"'{nameof(TestEvent)}' message never received for correlation id '{correlationId}'");

            Wait.UntilIsNotNull(() =>
                    MockMessageProcessingBehaviour.CalledForMessages.FirstOrDefault(m =>
                        m == correlationId),
                $"'{nameof(TestEvent)}' message never processed by MockMessageProcessingBehaviour for correlation id '{correlationId}'");

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Token.WaitHandle.WaitOne();

            await subscriberListeningTask;
        }

        [Fact]
        public async Task Given_A_CustomBatchProcessingStepIsAdded_WhenMessageProcessingIsInvoked_ThenTheCustomStepIsCalled()
        {
            var container = new Container(x =>
            {
                x.AddRegistry(new PatLiteRegistryBuilder(_subscriberConfiguration)
                    .WithMessageDeserialiser(ctx => ctx.GetInstance<MessageContext>().MessageEncrypted
                        ? new EncryptedMessageDeserialiser(ctx.GetInstance<DataProtectionConfiguration>())
                        : (IMessageDeserialiser)new NewtonsoftMessageDeserialiser())
                    .DefineBatchPipeline
                        .With<MockBatchProcessingBehaviour>()
                        .With<DefaultBatchProcessingBehaviour>()
                    .Build());
            });
            InitialiseIoC(container);

            var correlationId = Guid.NewGuid().ToString();

            container.Configure(x => x.For<MockBatchProcessingBehaviour.MockBatchProcessingBehaviourSettings>()
                .Use(context => new MockBatchProcessingBehaviour.MockBatchProcessingBehaviourSettings
                {
                    CorrelationId = correlationId
                }));

            var subscriber = container.GetInstance<Subscriber>();
            var cancellationTokenSource = new CancellationTokenSource();

            await subscriber.Initialise(new[] { typeof(SubscriberTests).Assembly });

            var subscriberListeningTask = Task.Run(() => subscriber.ListenForMessages(cancellationTokenSource));

            var messagePublisher = container.GetInstance<IMessagePublisher>();

            await messagePublisher.PublishEvent(new TestEvent(), new MessageProperties(correlationId));

            Wait.UntilIsNotNull(() =>
                    container.GetInstance<CapturedEvents>().ReceivedEvents
                        .FirstOrDefault(m => m.CorrelationId == correlationId),
                $"'{nameof(TestEvent)}' message never received for correlation id '{correlationId}'");

            Wait.UntilIsNotNull(() =>
                    MockBatchProcessingBehaviour.CalledForMessages.FirstOrDefault(m =>
                        m == correlationId),
                $"'{nameof(TestEvent)}' message never processed by MockMessageProcessingBehaviour for correlation id '{correlationId}'");

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Token.WaitHandle.WaitOne();

            await subscriberListeningTask;
        }
    }
}
