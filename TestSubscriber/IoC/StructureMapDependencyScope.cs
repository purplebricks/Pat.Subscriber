using System;
using System.Collections.Generic;
using System.Linq;
using PB.ITOps.Messaging.PatLite.IoC;
using StructureMap;

namespace TestSubscriber.IoC
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

        public object GetService(Type serviceType)
        {
            return Container.GetInstance(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return Container.GetAllInstances(serviceType).Cast<object>();
        }
    }
}