using System;
using System.Collections.Generic;

namespace Pat.Subscriber.IoC
{
    public interface IMessageDependencyScope : IDisposable
    {
        /// <summary>Retrieves a service from the scope.</summary>
        /// <returns>The retrieved service.</returns>
        T GetService<T>();

        /// <summary>Retrieves a service from the scope.</summary>
        /// <returns>The retrieved service.</returns>
        /// <param name="serviceType">The service to be retrieved.</param>
        object GetService(Type serviceType);

        /// <summary>Retrieves a collection of services from the scope.</summary>
        /// <returns>The retrieved collection of services.</returns>
        /// <param name="serviceType">The collection of services to be retrieved.</param>
        IEnumerable<object> GetServices(Type serviceType);

        /// <summary>Retrieves a collection of services from the scope.</summary>
        /// <returns>The retrieved collection of services.</returns>
        IEnumerable<T> GetServices<T>();
    }
}