namespace PB.ITOps.Messaging.PatLite.StructureMap4
{
    public class PatLiteOptions
    {
        public SubscriberConfiguration SubscriberConfiguration { get; set; }
        public PatLiteGlobalPolicyBuilder GlobalPolicyBuilder { get; set; }
        public PatLiteMessagePolicyBuilder MessagePolicyBuilder { get; set; }
    }
}