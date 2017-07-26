using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.PatLite.GlobalSubscriberPolicy;

namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
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

        public void RegisterPolicies(IServiceCollection serviceCollection)
        {
            foreach (var policyType in _policies)
            {
                serviceCollection.AddTransient(policyType);
            }
        }

        public ISubscriberPolicy Build(IServiceProvider provider)
        {
            ISubscriberPolicy policy = null;
            ISubscriberPolicy currentPolicy = null;

            foreach (var policyType in _policies)
            {
                var previousPolicy = (ISubscriberPolicy)provider.GetService(policyType);
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
