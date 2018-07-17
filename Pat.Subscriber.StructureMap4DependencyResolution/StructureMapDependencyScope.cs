using System;
using System.Collections.Generic;
using System.Linq;
using Pat.Subscriber.IoC;
using StructureMap;

namespace Pat.Subscriber.StructureMap4DependencyResolution
{
    public class StructureMapDependencyScope : IMessageDependencyScope
    {

        protected readonly IContainer Container;

        public StructureMapDependencyScope(IContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            Container = container;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Container.Dispose();
        }

        /// <inheritdoc/>
        public T GetService<T>()
        {
            return Container.GetInstance<T>();
        }

        /// <inheritdoc/>
        public object GetService(Type serviceType)
        {
            return Container.GetInstance(serviceType);
        }

        /// <inheritdoc/>
        public IEnumerable<object> GetServices(Type serviceType)
        {
            return Container.GetAllInstances(serviceType).Cast<object>();
        }

        /// <inheritdoc/>
        public IEnumerable<T> GetServices<T>()
        {
            return Container.GetAllInstances<T>();
        }
    }
}