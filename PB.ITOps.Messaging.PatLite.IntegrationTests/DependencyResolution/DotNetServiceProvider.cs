using System;
using Microsoft.Extensions.DependencyInjection;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests.DependencyResolution
{
    public class DotNetServiceProvider : IGenericServiceProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public DotNetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public T GetService<T>()
        {
            return ServiceProviderServiceExtensions.GetService<T>(_serviceProvider);
        }
    }
}