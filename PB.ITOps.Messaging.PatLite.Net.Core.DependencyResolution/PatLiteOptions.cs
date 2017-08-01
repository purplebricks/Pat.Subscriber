namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
{
    public class PatLiteOptions
    {
        public SubscriberConfiguration SubscriberConfiguration { get; set; }
        public PatLiteGlobalPolicyBuilder GlobalPolicyBuilder { get; set; }
        public AssemblyScanner AssemblyScanner { get; set; }
        public PatLiteMessagePolicyBuilder MessagePolicyBuilder { get; set; }
    }
}