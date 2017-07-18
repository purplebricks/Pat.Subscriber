namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
{
    public class PatLiteOptions
    {
        public SubscriberConfig SubscriberConfig { get; set; }
        public PatLitePolicyBuilder PolicyBuilder { get; set; }
        public AssemblyScanner AssemblyScanner { get; set; }
    }
}