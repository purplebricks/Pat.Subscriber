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

        public void Dispose()
        {
            Container.Dispose();
        }

        public T GetService<T>()
        {
            return Container.GetInstance<T>();
        }

        public object GetService(Type serviceType)
        {
            return Container.GetInstance(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return Container.GetAllInstances(serviceType).Cast<object>();
        }

        public IEnumerable<T> GetServices<T>()
        {
            return Container.GetAllInstances<T>();
        }
    }
}