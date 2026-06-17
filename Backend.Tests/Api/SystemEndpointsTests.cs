namespace Backend.Tests.Api;

using Core.Connection;
using Core.Version;
using static TestHarness;
using static VerifyTUnit.Verifier;

public class SystemEndpointsTests
{
	[Test]
	public async Task GetConnection_ReturnsRemoteUrlForTheLocalAddress()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.GetAsync("/system/connection");

		await Verify(await StatusAndJson<ConnectionInfo>(response));
	}

	[Test]
	public async Task GetVersion_ReturnsCurrentVersion()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.GetAsync("/system/version");

		await Verify(await StatusAndJson<VersionInfo>(response));
	}
}
