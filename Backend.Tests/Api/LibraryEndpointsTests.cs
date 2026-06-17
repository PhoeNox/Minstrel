namespace Backend.Tests.Api;

using Backend.Api;
using static TestHarness;
using static VerifyTUnit.Verifier;

public class LibraryEndpointsTests
{
	[Test]
	public async Task GetLibrary_ReturnsEverySongInThePool()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.GetAsync("/library");

		await Verify(await StatusAndJson<SongDto[]>(response));
	}
}
