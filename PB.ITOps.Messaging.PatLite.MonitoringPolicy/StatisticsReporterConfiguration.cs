namespace PB.ITOps.Messaging.PatLite.MonitoringPolicy
{
    public class StatisticsReporterConfiguration
    {
        public string Environment { get; set; }
        public string Tenant { get; set; }
        public int? StatsDPort { get; set; }
        public string StatsDHost { get; set; }
    }
}