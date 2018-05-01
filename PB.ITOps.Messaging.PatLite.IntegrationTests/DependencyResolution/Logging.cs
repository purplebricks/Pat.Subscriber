using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests.DependencyResolution
{
    public class Logging
    {
        public static void InitLogger(string loggerName = "IntegrationLogger")
        {

            Hierarchy hierarchy;
            if (!LoggerManager.RepositorySelector.ExistsRepository(loggerName))
            {
                hierarchy = (Hierarchy) LogManager.CreateRepository(loggerName);
            }
            else
            {
                hierarchy = (Hierarchy)LogManager.GetRepository(loggerName);
            }
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
            roller.File = $"{loggerName}.txt";
            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);

            hierarchy.Root.Level = Level.All;
            hierarchy.Configured = true;
        }
    }
}