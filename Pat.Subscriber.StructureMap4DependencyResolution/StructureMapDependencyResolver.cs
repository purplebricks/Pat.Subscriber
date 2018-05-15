using Pat.Subscriber.IoC;
using StructureMap;

namespace Pat.Subscriber.StructureMap4DependencyResolution
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
