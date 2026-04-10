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

	public Mock<IFileSystemProvider> FileSystemProvider { get; } = Mock.Of<IFileSystemProvider>();

	public Mock<INetworkProvider> NetworkProvider { get; } = Mock.Of<INetworkProvider>();

	public AppContext()
	{
		var serviceCollection = new ServiceCollection();

		var coreAssembly = typeof(Features.Playback.State).Assembly;
		serviceCollection.AddFluxor(o =>
		{
			o.ScanAssemblies(coreAssembly);
			o.WithLifetime(StoreLifetime.Singleton);
		});

		serviceCollection.AddSingleton(FileSystemProvider.Object);
		serviceCollection.AddSingleton(NetworkProvider.Object);
		serviceCollection.AddSingleton(new ConnectionOptions("5000"));
		serviceCollection.AddSingleton<TimeProvider, FakeTimeProvider>();

		FileSystemProvider.LoadSongs().Returns([]);
		FileSystemProvider.LoadPlaylists().Returns(([], []));

		Services = serviceCollection.BuildServiceProvider();
	}

	public Task InitializeAsync()
	{
		var store = Services.GetRequiredService<IStore>();
		return store.InitializeAsync();
	}
}
