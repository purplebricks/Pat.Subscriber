using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PB.ITOps.Messaging.PatLite.MessageProcessingPolicy;
using StructureMap;

namespace PB.ITOps.Messaging.PatLite.StructureMap4
{
    public class PatLiteMessagePolicyBuilder
    {
        private readonly ICollection<Type> _policies;

        public PatLiteMessagePolicyBuilder()
        {
            _policies = new Collection<Type>();
        }

        public PatLiteMessagePolicyBuilder AddPolicy<T>() where T : IMessageProcessingPolicy
        {
            _policies.Add(typeof(T));
            return this;
        }

        public void RegisterPolicies(Registry registry)
        {
            foreach (var policyType in _policies)
            {
                registry.AddType(policyType, policyType);
            }
        }

        public IMessageProcessingPolicy Build(IContext context)
        {
            IMessageProcessingPolicy policy = null;
            IMessageProcessingPolicy currentPolicy = null;

            foreach (var policyType in _policies)
            {
                var nextPolicy = (IMessageProcessingPolicy)context.GetInstance(policyType);
                if (policy == null)
                {
                    policy = nextPolicy;
                    currentPolicy = nextPolicy;
                }
                else
                {
                    currentPolicy.AppendPolicy(nextPolicy);
                    currentPolicy = nextPolicy;
                }
            }

            return policy;
        }
    }
}