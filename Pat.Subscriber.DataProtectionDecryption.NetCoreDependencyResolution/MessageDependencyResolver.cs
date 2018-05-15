using System;
using Microsoft.Extensions.DependencyInjection;
using PB.ITOps.Messaging.PatLite.IoC;

namespace PB.ITOps.Messaging.PatLite.Net.Core.DependencyResolution
{
    public class MessageDependencyResolver : MessageDependencyScope, IMessageDependencyResolver
    {
        public MessageDependencyResolver(IServiceProvider provider)
            : base(provider, (IServiceScope)null)
        {
        }

        public MessageDependencyResolver(IServiceProvider provider, IServiceScope scope)
            : base(provider, scope)
        {
        }

        public IMessageDependencyScope BeginScope()
        {
            var scope = Provider.CreateScope();
            return new MessageDependencyResolver(scope.ServiceProvider, scope);
        }
    }
}