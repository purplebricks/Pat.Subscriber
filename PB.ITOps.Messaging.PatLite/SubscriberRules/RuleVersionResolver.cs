using System;
using System.Reflection;

namespace PB.ITOps.Messaging.PatLite.SubscriberRules
{
    public class RuleVersionResolver: IRuleVersionResolver
    {
        public Version GetVersion()
        {
            return Assembly.GetEntryAssembly().GetName().Version;
        }
    }
}