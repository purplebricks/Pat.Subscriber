using StructureMap;

namespace PB.ITOps.Messaging.PatLite.IntegrationTests.DependencyResolution
{
    public class StructureMapServiceProvider : IGenericServiceProvider
    {
        private readonly IContainer _container;

        public StructureMapServiceProvider(IContainer container)
        {
            _container = container;
        }
        public T GetService<T>()
        {
            return _container.GetInstance<T>();
        }
    }
}