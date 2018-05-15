namespace Pat.Subscriber.IntegrationTests.DependencyResolution
{
    public interface IGenericServiceProvider
    {
        T GetService<T>();
    }
}