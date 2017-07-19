using System;
using log4net;
using PB.ITOps.Messaging.PatLite.IoC;
using PB.ITOps.Messaging.PatLite.MonitoringPolicy;
using PB.ITOps.Messaging.PatLite.Policy;
using StructureMap;

namespace PB.ITOps.Messaging.PatLite.StructureMap4
{
    public class PatLiteRegistry : Registry
    {
        private void CommonRegistrySetup()
        {
            Scan(scanner =>
            {
                scanner.WithDefaultConventions();
                scanner.AssemblyContainingType<IMessageContext>();
            });

            For<IMessageDependencyResolver>().Use<StructureMapDependencyResolver>();
            For<ILog>().AlwaysUnique().Use(s => LogManager.GetLogger(s.RootType));
            For<ISubscriberPolicy>().Use(c =>
                c.GetInstance<StandardPolicy>().ChainPolicy(c.GetInstance<MonitoringPolicy.MonitoringPolicy>())
            );
        }
        public PatLiteRegistry()
        {
            CommonRegistrySetup();
        }

        public PatLiteRegistry(SubscriberConfiguration subscriberConfig)
        {
            CommonRegistrySetup();
            For<SubscriberConfiguration>().Use(subscriberConfig);
        }
    }
}
