namespace PB.ITOps.Messaging.PatLite.IntegrationTests
{
    public interface IGenericServiceProvider
    {
        T GetService<T>();
    }
}