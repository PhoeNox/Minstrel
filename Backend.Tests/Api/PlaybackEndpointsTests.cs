namespace Backend.Tests.Api;

using System.Net;
using System.Text.Json;
using Backend.Api;
using Core;
using FileSystem.Tests;
using static TestHarness;
using static VerifyTUnit.Verifier;

public class PlaybackEndpointsTests
{
	[Test]
	public async Task Play_PlaysCurrentSong_WhenActivePlaylistHasSongs()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playback/play", NullBody());

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
	}

	[Test]
	public async Task Play_RejectsRequest_WhenActivePlaylistIsEmpty()
	{
		await using var app = new MinstrelApp(EmptyLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playback/play", NullBody());

		await response.ShouldBeProblem(HttpStatusCode.BadRequest, "The active playlist is empty.");
	}

	[Test]
	public async Task Pause_AcceptsCommand()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playback/pause", content: null);

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
	}

	[Test]
	public async Task SwitchPhase_AcceptsCommand()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playback/switch-phase", content: null);

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
	}

	[Test]
	public async Task Select_AcceptsCommand()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playback/select", JsonBody("""{"phase":"Day","index":0}"""));

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
	}

	[Test]
	public async Task Select_RejectsRequest_WhenBodyIsMissing()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playback/select", NullBody());

		await response.ShouldBeProblem(HttpStatusCode.BadRequest, "A phase and index are required.");
	}

	[Test]
	public async Task SetGain_AcceptsCommand()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playback/set-gain", JsonBody("""{"phase":"Day","value":0.5}"""));

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
	}

	[Test]
	public async Task SetGain_RejectsRequest_WhenBodyIsMissing()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playback/set-gain", NullBody());

		await response.ShouldBeProblem(HttpStatusCode.BadRequest, "A phase and gain value are required.");
	}

	[Test]
	public async Task Audio_ServesFile_ForKnownSong()
	{
		var file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp3");
		await File.WriteAllBytesAsync(file, [0x49, 0x44, 0x33]);
		try
		{
			var song = new Song(file, Title: "T", Artist: "A", Length: TimeSpan.FromMinutes(1));
			await using var app = new MinstrelApp(new FakeFileSystem(day: [], night: [], alsoOnDisk: [song]));
			var client = app.CreateClient();

			var response = await client.GetAsync($"/playback/audio/{SongId.From(file)}");

			await response.ShouldHaveStatus(HttpStatusCode.OK);
			await Assert.That(response.Content.Headers.ContentType?.ToString()).IsEqualTo("audio/mpeg");
		}
		finally
		{
			File.Delete(file);
		}
	}

	[Test]
	public async Task Audio_NotFound_ForUnknownSong()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.GetAsync("/playback/audio/does-not-exist");

		await response.ShouldHaveStatus(HttpStatusCode.NotFound);
	}

	[Test]
	public async Task Sse_EmitsInitialSnapshot_OnSubscribe()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var data = await Sse.FirstData(client, "/playback/sse");
		var snapshot = JsonSerializer.Deserialize<StateSnapshot>(data!, Json);

		await Verify(snapshot);
	}
}
