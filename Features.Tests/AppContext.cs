namespace Features.Tests;

using Fluxor;
using Infrastructure.FileSystem;
using Infrastructure.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using TUnit.Core.Interfaces;

public class AppContext : IAsyncInitializer
{
	public ServiceProvider Services { get; }
	
	public IDispatcher Dispatcher => Services.GetRequiredService<IDispatcher>();

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
	}

	public Task InitializeAsync()
	{
		var store = Services.GetRequiredService<IStore>();
		return store.InitializeAsync();
	}
}
