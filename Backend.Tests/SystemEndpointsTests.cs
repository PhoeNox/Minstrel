namespace Backend.Tests;

using System.Net;
using System.Net.Http.Json;
using Backend.Tests.Helpers;
using Core.Connection;
using Core.Version;
using static Backend.Tests.Helpers.TestHarness;
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

		await response.ShouldHaveStatus(HttpStatusCode.OK);
		var version = await response.Content.ReadFromJsonAsync<VersionInfo>(Json);
		await Assert.That(version).IsNotNull();
		await Assert.That(version!.Version).IsNotEqualTo("");
	}
}
