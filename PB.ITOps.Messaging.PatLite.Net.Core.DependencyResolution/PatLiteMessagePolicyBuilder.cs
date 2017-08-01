using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.PatLite.MessageProcessingPolicy;

namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
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

        public void RegisterPolicies(IServiceCollection serviceCollection)
        {
            foreach (var policyType in _policies)
            {
                serviceCollection.AddScoped(policyType);
            }
        }

        public IMessageProcessingPolicy Build(IServiceProvider provider)
        {
            IMessageProcessingPolicy policy = null;
            IMessageProcessingPolicy currentPolicy = null;

            foreach (var policyType in _policies)
            {
                var nextPolicy = (IMessageProcessingPolicy)provider.GetService(policyType);
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