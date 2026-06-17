namespace Backend.Tests.Api;

using static TestHarness;
using static VerifyTUnit.Verifier;

public class PlaylistEndpointsTests
{
	[Test]
	public async Task Add_AcceptsCommand()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/add", JsonBody($$"""{"phase":"Day","songId":"{{AddableSongId}}"}"""));

		await Verify(await StatusAndText(response));
	}

	[Test]
	public async Task Add_RejectsRequest_WhenBodyIsMissing()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/add", NullBody());

		await Verify(await StatusAndText(response));
	}

	[Test]
	public async Task Remove_AcceptsCommand()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/remove", JsonBody("""{"phase":"Day","index":0}"""));

		await Verify(await StatusAndText(response));
	}

	[Test]
	public async Task Remove_RejectsRequest_WhenBodyIsMissing()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/remove", NullBody());

		await Verify(await StatusAndText(response));
	}

	[Test]
	public async Task Shuffle_AcceptsCommand()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/shuffle", JsonBody("""{"phase":"Day"}"""));

		await Verify(await StatusAndText(response));
	}

	[Test]
	public async Task Shuffle_RejectsRequest_WhenBodyIsMissing()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/shuffle", NullBody());

		await Verify(await StatusAndText(response));
	}

	[Test]
	public async Task Move_AcceptsCommand()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/move", JsonBody("""{"phase":"Day","oldIndex":0,"newIndex":1}"""));

		await Verify(await StatusAndText(response));
	}

	[Test]
	public async Task Move_RejectsRequest_WhenBodyIsMissing()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/move", NullBody());

		await Verify(await StatusAndText(response));
	}
}
