using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Pat.Subscriber.BatchProcessing;
using Pat.Subscriber.IntegrationTests.DependencyResolution;
using Pat.Subscriber.IntegrationTests.Helpers;
using Pat.Subscriber.MessageProcessing;
using Pat.Subscriber.StructureMap4DependencyResolution;
using Pat.Subscriber.Telemetry.StatsD;
using StructureMap;
using Xunit;

namespace Pat.Subscriber.IntegrationTests
{
    public class StructureMapSubscriberTests
    {
        private readonly IConfigurationRoot _configuration;
        private readonly SubscriberConfiguration _subscriberConfiguration;
        private IStatisticsReporter _statisticsReporter;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly string _correlationId;

        private static async Task<Task> StartSubscriber(CancellationTokenSource cancellationTokenSource, Container container)
        {
            var subscriber = container.GetInstance<Subscriber>();
            await subscriber.Initialise(new[] { typeof(SubscriberTests).Assembly });

            var subscriberListeningTask = Task.Run(() => subscriber.ListenForMessages(cancellationTokenSource));
            return subscriberListeningTask;
        }

        private Container SetupContainer(Action<ConfigurationExpression> configurationExpression)
        {
            var container = new Container(configurationExpression);
            InitialiseIoC(container);
            return container;
        }

        public StructureMapSubscriberTests()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile(@"Configuration\appsettings.json");
            _configuration = configurationBuilder.Build();

            _subscriberConfiguration = new SubscriberConfiguration();
            _configuration.GetSection("PatLite:Subscriber").Bind(_subscriberConfiguration);
            _subscriberConfiguration.SubscriberName = $"{_subscriberConfiguration.SubscriberName}-StructureMap";
            _cancellationTokenSource = new CancellationTokenSource();
            _correlationId = Guid.NewGuid().ToString();
        }

        public IContainer InitialiseIoC(Container container)
        {
            var statisticsConfiguration = new StatisticsReporterConfiguration();
            _configuration.GetSection("StatsD").Bind(statisticsConfiguration);
            _statisticsReporter = Substitute.For<IStatisticsReporter>();

            var loggerName = "IntegrationLogger-StructureMap";
            Logging.InitLogger(loggerName);

            container.Configure(x =>
            {
                x.Scan(scanner =>
                {
                    scanner.WithDefaultConventions();
                });

                x.For<IStatisticsReporter>().Use(_statisticsReporter);
                x.For<MessageReceivedNotifier<TestEvent>>().Use(new MessageReceivedNotifier<TestEvent>());
                x.For<ILog>().Use(LogManager.GetLogger(loggerName, loggerName));
            });

            return container;
        }

        [Fact]
        public async Task Given_DefaultPatLiteRegistryBuilder_When_MessagePublished_HandlerReceivesMessageWithCorrectCorrelationId()
        {
            var container = SetupContainer(x =>
            {
                x.AddRegistry(new PatLiteRegistryBuilder(_subscriberConfiguration)
                    .WithDefaultPatLogger()
                    .Build());
                x.ForSingletonOf<ILoggerFactory>().Use(s => new LoggerFactory());
            });
            container.SetupTestMessage(_correlationId);

            var messageWaiter = container.GetInstance<MessageWaiter<TestEvent>>();
           
            var subscriberListeningTask = await StartSubscriber(_cancellationTokenSource, container);
            
            Assert.NotNull(messageWaiter.WaitOne());

            _cancellationTokenSource.Cancel();
            await subscriberListeningTask;
        }

        [Fact]
        public async Task Given_DefaultPatLiteRegistryBuilder_WhenHandlerProcessesMessage_ThenMonitoringIncrementsMessageProcessed()
        {
            var container = SetupContainer(x =>
            {
                x.AddRegistry(new PatLiteRegistryBuilder(_subscriberConfiguration)
                    .WithDefaultPatLogger()
                    .Build());
                x.ForSingletonOf<ILoggerFactory>().Use(s => new LoggerFactory());
            });
            container.SetupTestMessage(_correlationId);

            var messageWaiter = container.GetInstance<MessageWaiter<TestEvent>>();

            var subscriberListeningTask = await StartSubscriber(_cancellationTokenSource, container);

            Assert.NotNull(messageWaiter.WaitOne());

            _cancellationTokenSource.Cancel();
            await subscriberListeningTask;

            _statisticsReporter.Received()
                .Increment(
                    Arg.Is<string>(e => e.Equals("MessageProcessed", StringComparison.InvariantCultureIgnoreCase)),
                    Arg.Any<string>());
        }

        [Fact]
        public async Task
            Given_A_CustomMessageProcessingStepIsAdded_WhenMessageProcessingIsInvoked_ThenTheCustomStepIsCalled()
        {
            var container = SetupContainer(x =>
            {
                x.AddRegistry(new PatLiteRegistryBuilder(_subscriberConfiguration)
                    .WithDefaultPatLogger()
                    .DefineMessagePipeline
                        .With<DefaultMessageProcessingBehaviour>()
                        .With<MockMessageProcessingBehaviour>()
                        .With<InvokeHandlerBehaviour>()
                    .Build());
                x.ForSingletonOf<ILoggerFactory>().Use(s => new LoggerFactory());
            });
            container.SetupTestMessage(_correlationId);

            var messageWaiter = container.GetInstance<MessageWaiter<TestEvent>>();

            var subscriberListeningTask = await StartSubscriber(_cancellationTokenSource, container);

            Assert.True(messageWaiter.WaitOne() != null, $"'{nameof(TestEvent)}' message never received for correlation id '{_correlationId}'");

            _cancellationTokenSource.Cancel();
            await subscriberListeningTask;

            Assert.NotNull(MockMessageProcessingBehaviour.CalledForMessages.FirstOrDefault(m => m == _correlationId));
        }

        [Fact]
        public async Task Given_A_CustomBatchProcessingStepIsAdded_WhenMessageProcessingIsInvoked_ThenTheCustomStepIsCalled()
        {
            var container = SetupContainer(x =>
            {
                x.AddRegistry(new PatLiteRegistryBuilder(_subscriberConfiguration)
                    .WithDefaultPatLogger()
                    .DefineBatchPipeline
                        .With<MockBatchProcessingBehaviour>()
                        .With<DefaultBatchProcessingBehaviour>()
                    .Build());
                x.ForSingletonOf<ILoggerFactory>().Use(s => new LoggerFactory());
            });
            container.Configure(x => x.For<MockBatchProcessingBehaviour>()
                .Use(context => new MockBatchProcessingBehaviour(_correlationId)));
            container.SetupTestMessage(_correlationId);

            var messageWaiter = container.GetInstance<MessageWaiter<TestEvent>>();

            var subscriberListeningTask = await StartSubscriber(_cancellationTokenSource, container);
         
            Assert.True(messageWaiter.WaitOne()!=null, $"'{nameof(TestEvent)}' message never received for correlation id '{_correlationId}'");

            _cancellationTokenSource.Cancel();
            await subscriberListeningTask;

            Assert.NotNull(MockBatchProcessingBehaviour.CalledForMessages.FirstOrDefault(m => m == _correlationId));
        }
    }
}
