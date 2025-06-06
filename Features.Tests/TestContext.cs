namespace Features.Tests;

using Fluxor;
using Infrastructure.FileSystem;
using Microsoft.Extensions.DependencyInjection;

public class TestContext
{
    public ServiceProvider Services { get; }
    
    public Mock<IPlaylistLoader> PlaylistLoader { get; } = new();

    public TestContext()
    {
        var serviceCollection = new ServiceCollection();
        
        var coreAssembly = typeof(Playback.State).Assembly;
        serviceCollection.AddFluxor(o =>
        {
            o.ScanAssemblies(coreAssembly);
            o.WithLifetime(StoreLifetime.Singleton);
        });
        
        serviceCollection.AddSingleton(PlaylistLoader.Object);
        
        Services = serviceCollection.BuildServiceProvider();
        
        var store = Services.GetRequiredService<IStore>();
        store.InitializeAsync().Wait();
    }
}
