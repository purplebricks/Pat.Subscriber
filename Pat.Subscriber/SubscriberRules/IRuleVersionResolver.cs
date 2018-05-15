using System;

namespace Pat.Subscriber.SubscriberRules
{
    public interface IRuleVersionResolver
    {
        Version GetVersion();
    }
}