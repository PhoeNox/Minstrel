namespace Backend.Tests.Helpers;

using FileSystem;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Network;

// Boots the real Backend in-process over a TestServer, swapping the two
// environment-dependent edges (file system, local-IP detection) for deterministic,
// parallel-safe fakes. Everything else — DI wiring, sessions, endpoints, mappers — is real.
public sealed class MinstrelApp(IFileSystemProvider fileSystem) : WebApplicationFactory<Program>
{
	// The entry point opens the Player in a browser on startup unless LaunchBrowser is off.
	// It reads that key straight from configuration before any ConfigureWebHost override
	// lands, so the suppression has to be an environment variable set before the host builds.
	static MinstrelApp() => Environment.SetEnvironmentVariable("LaunchBrowser", "false");

	private readonly INetworkProvider network = new FakeNetwork("192.168.1.50");

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");
		builder.ConfigureTestServices(services =>
		{
			services.RemoveAll<IHostedService>();
			services.RemoveAll<IFileSystemProvider>();
			services.AddSingleton(fileSystem);
			services.RemoveAll<INetworkProvider>();
			services.AddSingleton(network);
		});
	}
}
