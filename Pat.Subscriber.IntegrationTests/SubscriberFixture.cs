using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pat.Subscriber.IntegrationTests.DependencyResolution;
using Pat.Subscriber.IntegrationTests.Helpers;

namespace Pat.Subscriber.IntegrationTests
{
    public class SubscriberFixture : IDisposable
    {
        public IGenericServiceProvider ServiceProvider { get; }
        public bool IntegrationTest { get; }
        public bool AppVeyorCIBuild { get; }

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Task _subscriberTask;

        public SubscriberFixture()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile(@"Configuration\appsettings.json");
            var configuration = configurationBuilder.Build();

            IntegrationTest = bool.Parse(configuration["SubscriberTests:IntegrationTest"]);

            AppVeyorCIBuild = bool.Parse(Environment.GetEnvironmentVariable("APPVEYOR") ?? "false");

            if (AppVeyorCIBuild)
            {
                return;
            }

            if (bool.Parse(configuration["SubscriberTests:UseStructureMap"]))
            {
                var container = StructureMapIoC.Initialize(configuration);
                if (!IntegrationTest)
                {
                    container.SetupTestMessage(null);
                }
                container.Configure(x =>
                    {
                        x.For<TestMessageSender>().Use(context =>
                            new TestMessageSender(context.GetInstance<IGenericServiceProvider>(), IntegrationTest));
                        x.For<IGenericServiceProvider>().Use(new StructureMapServiceProvider(container));
                    }
                );
                ServiceProvider = container.GetInstance<IGenericServiceProvider>();
            }
            else
            {
                var serviceCollection = DotNetIoC.Initialize(configuration);
                if (!IntegrationTest)
                {
                    serviceCollection.SetupTestMessage(null);
                }
                serviceCollection.AddSingleton(provider =>
                    new TestMessageSender(provider.GetService<IGenericServiceProvider>(), IntegrationTest));
                serviceCollection.AddSingleton<IGenericServiceProvider>(provider => new DotNetServiceProvider(provider));
                var serviceProvider = serviceCollection.BuildServiceProvider();
                ServiceProvider = serviceProvider.GetService<IGenericServiceProvider>();
            }

            var subscriber = ServiceProvider.GetService<Subscriber>();
            
            if (subscriber.Initialise(new[] {typeof(SubscriberTests).Assembly}).GetAwaiter().GetResult())
            {
                _subscriberTask = Task.Run(() => subscriber.ListenForMessages(_cancellationTokenSource));
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _subscriberTask?.Wait();
        }
    }
}