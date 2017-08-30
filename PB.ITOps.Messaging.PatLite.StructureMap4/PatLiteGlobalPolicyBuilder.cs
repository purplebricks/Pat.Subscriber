using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PB.ITOps.Messaging.PatLite.GlobalSubscriberPolicy;
using StructureMap;

namespace PB.ITOps.Messaging.PatLite.StructureMap4
{
    public class PatLiteGlobalPolicyBuilder
    {
        private readonly ICollection<Type> _policies;

        public PatLiteGlobalPolicyBuilder()
        {
            _policies = new Collection<Type>();
        }

        public PatLiteGlobalPolicyBuilder AddPolicy<T>() where T : ISubscriberPolicy
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

        public ISubscriberPolicy Build(IContext context)
        {
            ISubscriberPolicy policy = null;
            ISubscriberPolicy currentPolicy = null;

            foreach (var policyType in _policies)
            {
                var previousPolicy = (ISubscriberPolicy)context.GetInstance(policyType);
                if (policy == null)
                {
                    policy = previousPolicy;
                    currentPolicy = previousPolicy;
                }
                else
                {
                    currentPolicy.AppendInnerPolicy(previousPolicy);
                    currentPolicy = previousPolicy;
                }
            }

            return policy;
        }
    }
}