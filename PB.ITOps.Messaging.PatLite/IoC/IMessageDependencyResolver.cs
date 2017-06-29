using System;

namespace PB.ITOps.Messaging.PatLite.IoC
{
    public interface IMessageDependencyResolver : IDisposable
    {
        IMessageDependencyScope BeginScope();
    }
}
