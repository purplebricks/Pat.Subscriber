using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.PatLite.Policy;

namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
{
    public class PatLitePolicyBuilder
    {
        private readonly ICollection<Type> _policies;

        public PatLitePolicyBuilder()
        {
            _policies = new Collection<Type>();
        }

        public PatLitePolicyBuilder AddPolicy<T>() where T : ISubscriberPolicy
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
                    currentPolicy.ChainPolicy(previousPolicy);
                    currentPolicy = previousPolicy;
                }
            }

            return policy;
        }
    }
}
