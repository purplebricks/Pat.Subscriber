namespace PB.ITOps.Messaging.PatLite.IntegrationTests.DependencyResolution
{
    public interface IGenericServiceProvider
    {
        T GetService<T>();
    }
}