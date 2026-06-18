namespace Backend.Tests;

using System.Net;
using Backend.Tests.Helpers;
using Core;
using static Backend.Tests.Helpers.TestHarness;

public class PlaylistEndpointsTests
{
	[Test]
	public async Task Add_AppendsSongToPlaylist()
	{
		var fileSystem = SeededLibrary();
		await using var app = new MinstrelApp(fileSystem);
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/add", JsonBody($$"""{"phase":"Day","songId":"{{AddableSongId}}"}"""));

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
		await Assert.That(Saved(fileSystem)).IsEqualTo("a.mp3,b.mp3,c.mp3");
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
	public async Task Add_NotFound_WhenSongIsNotInTheLibrary()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/add", JsonBody("""{"phase":"Day","songId":"nope"}"""));

		await response.ShouldBeProblem(HttpStatusCode.NotFound, "No song with id 'nope' in the library.");
	}

	[Test]
	public async Task Remove_DropsSongFromPlaylist()
	{
		var fileSystem = SeededLibrary();
		await using var app = new MinstrelApp(fileSystem);
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/remove", JsonBody("""{"phase":"Day","index":0}"""));

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
		await Assert.That(Saved(fileSystem)).IsEqualTo("b.mp3");
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
	public async Task Remove_RejectsRequest_WhenIndexIsOutOfRange()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/remove", JsonBody("""{"phase":"Day","index":5}"""));

		await response.ShouldBeProblem(HttpStatusCode.BadRequest, "The playlist index is out of range.");
	}

	[Test]
	public async Task Shuffle_PersistsThePlaylistsSongs()
	{
		var fileSystem = SeededLibrary();
		await using var app = new MinstrelApp(fileSystem);
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/shuffle", JsonBody("""{"phase":"Day"}"""));

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
		await Assert.That(SavedSorted(fileSystem)).IsEqualTo("a.mp3,b.mp3");
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
	public async Task Move_ReordersThePlaylist()
	{
		var fileSystem = SeededLibrary();
		await using var app = new MinstrelApp(fileSystem);
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/move", JsonBody("""{"phase":"Day","oldIndex":0,"newIndex":1}"""));

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
		await Assert.That(Saved(fileSystem)).IsEqualTo("b.mp3,a.mp3");
	}

	[Test]
	public async Task Move_RejectsRequest_WhenIndexIsOutOfRange()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/move", JsonBody("""{"phase":"Day","oldIndex":5,"newIndex":0}"""));

		await response.ShouldBeProblem(HttpStatusCode.BadRequest, "The playlist index is out of range.");
	}

	[Test]
	public async Task Move_RejectsRequest_WhenBodyIsMissing()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playlist/move", NullBody());

		await response.ShouldBeProblem(HttpStatusCode.BadRequest, "A phase and indices are required.");
	}

	// The persisted Day playlist as the M3U writer left it: relative paths, in order.
	private static string Saved(FileSystem.Tests.FakeFileSystem fileSystem)
		=> string.Join(",", fileSystem.Saved(GamePhase.Day));

	private static string SavedSorted(FileSystem.Tests.FakeFileSystem fileSystem)
		=> string.Join(",", fileSystem.Saved(GamePhase.Day).Order());
}
