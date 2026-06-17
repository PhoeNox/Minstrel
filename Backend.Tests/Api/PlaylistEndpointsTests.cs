namespace Backend.Tests.Api;

using System.Net;
using static TestHarness;

public class PlaylistEndpointsTests
{
	[Test]
	public async Task Add_AcceptsCommand()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/add", JsonBody($$"""{"phase":"Day","songId":"{{AddableSongId}}"}"""));

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
	}

	[Test]
	public async Task Add_RejectsRequest_WhenBodyIsMissing()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/add", NullBody());

		await response.ShouldBeProblem(HttpStatusCode.BadRequest, "A phase and song id are required.");
	}

	[Test]
	public async Task Remove_AcceptsCommand()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/remove", JsonBody("""{"phase":"Day","index":0}"""));

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
	}

	[Test]
	public async Task Remove_RejectsRequest_WhenBodyIsMissing()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/remove", NullBody());

		await response.ShouldBeProblem(HttpStatusCode.BadRequest, "A phase and index are required.");
	}

	[Test]
	public async Task Shuffle_AcceptsCommand()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/shuffle", JsonBody("""{"phase":"Day"}"""));

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
	}

	[Test]
	public async Task Shuffle_RejectsRequest_WhenBodyIsMissing()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/shuffle", NullBody());

		await response.ShouldBeProblem(HttpStatusCode.BadRequest, "A phase is required.");
	}

	[Test]
	public async Task Move_AcceptsCommand()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/move", JsonBody("""{"phase":"Day","oldIndex":0,"newIndex":1}"""));

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
	}

	[Test]
	public async Task Move_RejectsRequest_WhenBodyIsMissing()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/move", NullBody());

		await response.ShouldBeProblem(HttpStatusCode.BadRequest, "A phase and indices are required.");
	}
}
