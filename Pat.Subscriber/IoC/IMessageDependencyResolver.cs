using System;

namespace Pat.Subscriber.IoC
{
    public interface IMessageDependencyResolver : IDisposable
    {
        IMessageDependencyScope BeginScope();
    }
}
