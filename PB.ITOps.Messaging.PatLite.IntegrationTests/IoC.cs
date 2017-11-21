using System;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution;
using PB.ITOps.Messaging.PatSender;
using PB.ITOps.Messaging.PatSender.MessageGeneration;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests
{
    public class IoC
    {
        public static IServiceProvider Initialize(IConfigurationRoot configuration)
        {
            var senderSettings = new PatSenderSettings();
            configuration.GetSection("PatLite:Sender").Bind(senderSettings);

            var subscriberSettings = new SubscriberConfiguration();
            configuration.GetSection("PatLite:Subscriber").Bind(subscriberSettings);
            InitLogger();
            var serviceProvider = new ServiceCollection()
                .AddSingleton(senderSettings)
                .AddSingleton(subscriberSettings)
                .AddSingleton<IMessageGenerator, MessageGenerator>()
                .AddTransient<IMessagePublisher>(
                    provider => new MessagePublisher(
                        provider.GetRequiredService<IMessageSender>(),
                        provider.GetRequiredService<IMessageGenerator>(),
                        new MessageProperties(Guid.NewGuid().ToString())))
                .AddSingleton(LogManager.GetLogger("IntegrationLogger"))
                .AddTransient<IMessageSender, MessageSender>()
                .AddPatLite()
                .AddHandlersFromAssemblyContainingType<IoC>()
                .BuildServiceProvider();

            return serviceProvider;
        }

        private static void InitLogger()
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();
            var tracer = new TraceAppender();
            var patternLayout = new PatternLayout();

            patternLayout.ConversionPattern = "%d [%t] %-5p %m%n";
            patternLayout.ActivateOptions();

            tracer.Layout = patternLayout;
            tracer.ActivateOptions();
            hierarchy.Root.AddAppender(tracer);

            var roller = new RollingFileAppender();
            roller.Layout = patternLayout;
            roller.AppendToFile = true;
            roller.RollingStyle = RollingFileAppender.RollingMode.Size;
            roller.MaxSizeRollBackups = 4;
            roller.MaximumFileSize = "100KB";
            roller.StaticLogFileName = true;
            roller.File = "IntegrationLogger.txt";
            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);

            hierarchy.Root.Level = Level.All;
            hierarchy.Configured = true;
        }
    }
}