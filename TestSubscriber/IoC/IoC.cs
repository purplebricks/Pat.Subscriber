using log4net;
using PB.ITOps.Messaging.PatLite.IoC;
using PB.ITOps.Messaging.PatSender;
using StructureMap;

namespace TestSubscriber.IoC
{
    public static class IoC
    {
        public static IContainer Initialize()
        {
            var container = new Container(x =>
            {
                x.Scan(scanner =>
                {
                    scanner.WithDefaultConventions();
                    scanner.AssemblyContainingType<IMessageSender>();
                    scanner.AssemblyContainingType<IMessageContext>();
                });

                x.For<ILog>().AlwaysUnique().Use(s => LogManager.GetLogger(s.RootType));
                x.For<IMessagePublisher>().Use<MessagePublisher>().Ctor<string>().Is((context) => context.GetInstance<IMessageContext>().CorrelationId);
            });

            return container;
        }
    }
}
