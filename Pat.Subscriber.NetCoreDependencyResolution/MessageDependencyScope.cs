using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Pat.Subscriber.IoC;

namespace Pat.Subscriber.NetCoreDependencyResolution
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

        /// <inheritdoc/>
        public void Dispose()
        {
            Scope?.Dispose();
        }

        /// <inheritdoc/>
        public T GetService<T>()
        {
            return Provider.GetService<T>();
        }

        /// <inheritdoc/>
        public object GetService(Type serviceType)
        {
            return Provider.GetService(serviceType);
        }

        /// <inheritdoc/>
        public IEnumerable<object> GetServices(Type serviceType)
        {
            return Provider.GetServices(serviceType).Cast<object>();
        }

        /// <inheritdoc/>
        public IEnumerable<T> GetServices<T>()
        {
            return Provider.GetServices<T>();
        }
    }
}