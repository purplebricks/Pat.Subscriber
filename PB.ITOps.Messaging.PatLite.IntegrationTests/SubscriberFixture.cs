using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests
{
    public class SubscriberFixture : IDisposable
    {
        public IGenericServiceProvider ServiceProvider { get; }

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _subscriberTask;

        public SubscriberFixture()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile(@"Configuration\appsettings.json");
            var configuration = configurationBuilder.Build();

            if (bool.Parse(configuration["UseStructureMap"]))
            {
                ServiceProvider = new StructureMapServiceProvider(StructureMapIoC.Initialize(configuration));
            }
            else
            {
                ServiceProvider = new DotNetServiceProvider(DotNetIoC.Initialize(configuration));
            }

            var subscriber = ServiceProvider.GetService<Subscriber>();
            _cancellationTokenSource = new CancellationTokenSource();
            if (subscriber.Initialise(new[] {typeof(SubscriberTests).Assembly}).GetAwaiter().GetResult())
            {
                _subscriberTask = Task.Run(() => subscriber.ListenForMessages(_cancellationTokenSource));
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Token.WaitHandle.WaitOne();

            _subscriberTask.Wait();
        }
    }
}