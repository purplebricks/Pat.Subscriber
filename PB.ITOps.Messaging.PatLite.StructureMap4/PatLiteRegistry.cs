using log4net;
using PB.ITOps.Messaging.PatLite.GlobalSubscriberPolicy;
using PB.ITOps.Messaging.PatLite.IoC;
using PB.ITOps.Messaging.PatLite.MessageProcessingPolicy;
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
            
        }

        private void ConfigureDefaultProcessingPolicies()
        {
            For<ISubscriberPolicy>().Use(c =>
                c.GetInstance<StandardPolicy>()
                    .AppendInnerPolicy(c.GetInstance<MonitoringPolicy.MonitoringPolicy>())
            );
            For<IMessageProcessingPolicy>().Use<DefaultMessageProcessingPolicy>();
        }

        public PatLiteRegistry()
        {
            CommonRegistrySetup();
            ConfigureDefaultProcessingPolicies();
        }

        public PatLiteRegistry(SubscriberConfiguration subscriberConfig)
        {
            CommonRegistrySetup();
            ConfigureDefaultProcessingPolicies();
            For<SubscriberConfiguration>().Use(subscriberConfig);
        }

        public PatLiteRegistry(PatLiteOptions options)
        {
            CommonRegistrySetup();
            For<SubscriberConfiguration>().Use(options.SubscriberConfiguration);

            if (options.GlobalPolicyBuilder == null)
            {
                options.GlobalPolicyBuilder = new PatLiteGlobalPolicyBuilder()
                    .AddPolicy<MonitoringPolicy.MonitoringPolicy>()
                    .AddPolicy<StandardPolicy>();
            }

            if (options.MessagePolicyBuilder == null)
            {
                options.MessagePolicyBuilder = new PatLiteMessagePolicyBuilder()
                    .AddPolicy<DefaultMessageProcessingPolicy>();
            }

            options.GlobalPolicyBuilder.RegisterPolicies(this);
            options.MessagePolicyBuilder.RegisterPolicies(this);

            For<ISubscriberPolicy>().Use((ctx) => options.GlobalPolicyBuilder.Build(ctx));
            For<IMessageProcessingPolicy>().Use((ctx) => options.MessagePolicyBuilder.Build(ctx));

        }
    }
}
