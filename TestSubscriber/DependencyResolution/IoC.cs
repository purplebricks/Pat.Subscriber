using PB.ITOps.Messaging.PatLite;
using PB.ITOps.Messaging.PatLite.IoC;
using PB.ITOps.Messaging.PatLite.StructureMap4;
using PB.ITOps.Messaging.PatSender;
using StructureMap;

namespace TestSubscriber.DependencyResolution
{
    public class IoC: Registry
    {
        public static IContainer Initialize(SubscriberConfig config, PatSenderSettings patSenderConfig)
        {
            var container = new Container(x =>
            {
                x.AddRegistry(new PatLiteRegistry(config));
                x.For<IMessagePublisher>().Use<MessagePublisher>().Ctor<string>().Is((context) => context.GetInstance<IMessageContext>().CorrelationId);
                x.For<PatSenderSettings>().Use(patSenderConfig);
            });

            return container;
        }
    }
}
