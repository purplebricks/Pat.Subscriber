using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Pat.Subscriber.BatchProcessing;
using Pat.Subscriber.IntegrationTests.DependencyResolution;
using Pat.Subscriber.IntegrationTests.Helpers;
using Pat.Subscriber.MessageProcessing;
using Pat.Subscriber.NetCoreDependencyResolution;
using Pat.Subscriber.Telemetry.StatsD;
using Xunit;

namespace Pat.Subscriber.IntegrationTests
{
    public class DotNetIocSubscriberTests
    {
        private readonly IConfigurationRoot _configuration;
        private readonly SubscriberConfiguration _subscriberConfiguration;
        private IStatisticsReporter _statisticsReporter;
        private readonly string _correlationId;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private static async Task<Task> StartSubscriber(CancellationTokenSource cancellationTokenSource, IServiceProvider serviceProvider)
        {
            var subscriber = serviceProvider.GetService<Subscriber>();
            await subscriber.Initialise(new[] { typeof(SubscriberTests).Assembly });
            var subscriberListeningTask = Task.Run(() => subscriber.ListenForMessages(cancellationTokenSource));
            return subscriberListeningTask;
        }

        public DotNetIocSubscriberTests()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile(@"Configuration\appsettings.json");
            _configuration = configurationBuilder.Build();

            _subscriberConfiguration = new SubscriberConfiguration();
            _configuration.GetSection("PatLite:Subscriber").Bind(_subscriberConfiguration);
            _subscriberConfiguration.SubscriberName = $"{_subscriberConfiguration.SubscriberName}-DotNetIoC";
            _correlationId = Guid.NewGuid().ToString();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public IServiceProvider InitialiseIoC(IServiceCollection serviceCollection)
        {
            var statisticsConfiguration = new StatisticsReporterConfiguration();
            _configuration.GetSection("StatsD").Bind(statisticsConfiguration);
            _statisticsReporter = Substitute.For<IStatisticsReporter>();

            var serviceProvider = serviceCollection
                .AddSingleton(statisticsConfiguration)
                .AddSingleton<MessageReceivedNotifier<TestEvent>>()
                .AddSingleton(_statisticsReporter)
                .AddHandlersFromAssemblyContainingType<DotNetIoC>()
                .AddDefaultPatLogger()
                .AddLogging(b => b.AddDebug())
                .BuildServiceProvider();

            return serviceProvider;
        }

        [Fact]
        public async Task Given_AddPatLiteExtension_When_MessagePublished_HandlerReceivesMessageWithCorrectCorrelationId()
        {
            var collection = new ServiceCollection()
                .AddPatLite(_subscriberConfiguration);

            collection.SetupTestMessage(_correlationId);
            var serviceProvider = InitialiseIoC(collection);
            var messageWaiter = serviceProvider.GetService<MessageWaiter<TestEvent>>();

            var subscriberListeningTask = await StartSubscriber(_cancellationTokenSource, serviceProvider);
            
            Assert.NotNull(messageWaiter.WaitOne());

            _cancellationTokenSource.Cancel();
            await subscriberListeningTask;
        }

        [Fact]
        public async Task Given_AddPatLiteExtension_WhenHandlerProcessesMessage_ThenMonitoringIncrementsMessageProcessed()
        {
            var collection = new ServiceCollection()
                .AddPatLite(_subscriberConfiguration);

            collection.SetupTestMessage(_correlationId);
            var serviceProvider = InitialiseIoC(collection);
            var messageWaiter = serviceProvider.GetService<MessageWaiter<TestEvent>>();

            var subscriberListeningTask = await StartSubscriber(_cancellationTokenSource, serviceProvider);

            Assert.True(messageWaiter.WaitOne()!=null, $"'{nameof(TestEvent)}' message never received for correlation id '{_correlationId}'");

            _cancellationTokenSource.Cancel();
            await subscriberListeningTask;

            _statisticsReporter.Received().Increment(Arg.Is<string>(e => e.Equals("MessageProcessed", StringComparison.InvariantCultureIgnoreCase)), Arg.Any<string>());
        }

        [Fact]
        public async Task Given_A_CustomMessageProcessingStepIsAdded_WhenMessageProcessingIsInvoked_ThenTheCustomStepIsCalled()
        {
           var collection = new ServiceCollection()
                .AddPatLite(_subscriberConfiguration)
                .AddPatLite(new PatLiteOptionsBuilder(_subscriberConfiguration)
                    .DefineMessagePipeline
                        .With<DefaultMessageProcessingBehaviour>()
                        .With<MockMessageProcessingBehaviour>()
                        .With<InvokeHandlerBehaviour>()
                    .Build());

            collection.SetupTestMessage(_correlationId);
            var serviceProvider = InitialiseIoC(collection);
            var messageWaiter = serviceProvider.GetService<MessageWaiter<TestEvent>>();

            var subscriberListeningTask = await StartSubscriber(_cancellationTokenSource, serviceProvider);

            Assert.True(messageWaiter.WaitOne()!=null, $"'{nameof(TestEvent)}' message never received for correlation id '{_correlationId}'");
                    
            _cancellationTokenSource.Cancel();
            await subscriberListeningTask;

            Assert.NotNull(MockMessageProcessingBehaviour.CalledForMessages.FirstOrDefault(m => m == _correlationId));
        }

        [Fact]
        public async Task Given_A_CustomBatchProcessingStepIsAdded_WhenMessageProcessed_ThenTheCustomStepIsCalled()
        {
            var collection = new ServiceCollection()
                .AddPatLite(new PatLiteOptionsBuilder(_subscriberConfiguration)
                    .DefineBatchPipeline
                        .With<MockBatchProcessingBehaviour>()
                        .With<DefaultBatchProcessingBehaviour>()
                    .Build());
            collection.AddSingleton(provider => new MockBatchProcessingBehaviour(_correlationId));

            collection.SetupTestMessage(_correlationId);
            var serviceProvider = InitialiseIoC(collection);
            var messageWaiter = serviceProvider.GetService<MessageWaiter<TestEvent>>();

            var subscriberListeningTask = await StartSubscriber(_cancellationTokenSource, serviceProvider);       

            Assert.True(messageWaiter.WaitOne()!=null, $"'{nameof(TestEvent)}' message never received for correlation id '{_correlationId}'");

            _cancellationTokenSource.Cancel();
            await subscriberListeningTask;

            Assert.NotNull(MockBatchProcessingBehaviour.CalledForMessages.FirstOrDefault(m => m == _correlationId));
        }
    }
}
