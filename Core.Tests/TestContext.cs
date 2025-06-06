namespace Core.Tests;

using Fluxor;
using Microsoft.Extensions.DependencyInjection;

public class TestContext
{
    public ServiceProvider Services { get; }

    public TestContext()
    {
        var serviceCollection = new ServiceCollection();
        var coreAssembly = typeof(Song).Assembly;
        serviceCollection.AddFluxor(o =>
        {
            o.ScanAssemblies(coreAssembly);
            o.WithLifetime(StoreLifetime.Singleton);
        });
        
        Services = serviceCollection.BuildServiceProvider();
        
        var store = Services.GetRequiredService<IStore>();
        store.InitializeAsync().Wait();
    }
}
