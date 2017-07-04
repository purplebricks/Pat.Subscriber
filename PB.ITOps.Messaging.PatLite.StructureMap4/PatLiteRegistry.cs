using log4net;
using PB.ITOps.Messaging.PatLite.IoC;
using StructureMap;

namespace PB.ITOps.Messaging.PatLite.StructureMap4
{
    public class PatLiteRegistry : Registry
    {
        public PatLiteRegistry(SubscriberConfig config)
        {
            Scan(scanner =>
            {
                scanner.WithDefaultConventions();
                scanner.AssemblyContainingType<IMessageContext>();
            });

            For<IMessageDependencyResolver>().Use<StructureMapDependencyResolver>();
            For<SubscriberConfig>().Use(config);
            For<ILog>().AlwaysUnique().Use(s => LogManager.GetLogger(s.RootType));
        }
    }
}
