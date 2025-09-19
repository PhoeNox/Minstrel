namespace Features.Tests;

using Fluxor;
using Infrastructure.FileSystem;
using Infrastructure.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

public class AppContext
{
	public ServiceProvider Services { get; }

	public Mock<IPlaylistLoader> PlaylistLoader { get; } = new();
	
	public Mock<INetworkProvider> NetworkProvider { get; } = new();

	public AppContext()
	{
		var serviceCollection = new ServiceCollection();

		var coreAssembly = typeof(Features.Playback.State).Assembly;
		serviceCollection.AddFluxor(o =>
		{
			o.ScanAssemblies(coreAssembly);
			o.WithLifetime(StoreLifetime.Singleton);
		});

		serviceCollection.AddSingleton(PlaylistLoader.Object);
		serviceCollection.AddSingleton(NetworkProvider.Object);
		serviceCollection.AddSingleton<TimeProvider, FakeTimeProvider>();

		Services = serviceCollection.BuildServiceProvider();

		var store = Services.GetRequiredService<IStore>();
		store.InitializeAsync().Wait();
	}
}
