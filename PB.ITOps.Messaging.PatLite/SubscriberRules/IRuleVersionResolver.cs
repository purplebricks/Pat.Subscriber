using System;

namespace PB.ITOps.Messaging.PatLite.SubscriberRules
{
    public interface IRuleVersionResolver
    {
        Version GetVersion();
    }
}