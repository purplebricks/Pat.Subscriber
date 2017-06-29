using PB.ITOps.Messaging.PatLite.IoC;
using StructureMap;

namespace TestSubscriber.DependencyResolution
{
    public class StructureMapDependencyResolver : StructureMapDependencyScope, IMessageDependencyResolver
    { 
        public StructureMapDependencyResolver(IContainer container)
            : base(container)
        {
        }
      
        public IMessageDependencyScope BeginScope()
        {
            IContainer child = Container.GetNestedContainer();
            return new StructureMapDependencyResolver(child);
        }
    }
}
