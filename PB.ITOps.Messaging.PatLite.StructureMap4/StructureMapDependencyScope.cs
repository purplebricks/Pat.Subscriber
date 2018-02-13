using System;
using System.Collections.Generic;
using System.Linq;
using PB.ITOps.Messaging.PatLite.IoC;
using StructureMap;

namespace PB.ITOps.Messaging.PatLite.StructureMap4
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