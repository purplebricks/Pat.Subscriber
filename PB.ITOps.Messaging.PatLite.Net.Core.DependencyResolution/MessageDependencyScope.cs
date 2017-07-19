using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.PatLite.IoC;

namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
{
    public class MessageDependencyScope : IMessageDependencyScope
    {
        protected readonly IServiceProvider Provider;
        protected readonly IServiceScope Scope;
        public MessageDependencyScope(IServiceProvider provider, IServiceScope scope)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            Provider = provider;
            Scope = scope;
        }

        public void Dispose()
        {
            Scope?.Dispose();
        }

        public object GetService(Type serviceType)
        {
            return Provider.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return Provider.GetServices(serviceType).Cast<object>();
        }
    }
}