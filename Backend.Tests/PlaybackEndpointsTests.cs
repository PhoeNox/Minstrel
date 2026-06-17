namespace Backend.Tests;

using System.Net;
using System.Text.Json;
using Backend.Api;
using Backend.Tests.Helpers;
using Core;
using FileSystem.Tests;
using static Backend.Tests.Helpers.TestHarness;
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
		var state = await ReadPlayback(client);
		await Assert.That(state.IsPlaying).IsTrue();
		await Assert.That(state.CurrentSongId).IsEqualTo(FirstDaySongId);
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
	public async Task Pause_StopsPlayback()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();
		await client.PostAsync("/playback/play", NullBody());

		var response = await client.PostAsync("/playback/pause", content: null);

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
		var state = await ReadPlayback(client);
		await Assert.That(state.IsPlaying).IsFalse();
	}

	[Test]
	public async Task SwitchPhase_TogglesTheActivePhase()
	{
		// Switching is a no-op onto an empty phase, so Night needs a song to switch to.
		await using var app = new MinstrelApp(new FakeFileSystem(
			day: [TestSongs.At("a.mp3")],
			night: [TestSongs.At("d.mp3")]));
		var client = app.CreateClient();

		var response = await client.PostAsync("/playback/switch-phase", content: null);

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
		var state = await ReadPlayback(client);
		await Assert.That(state.Phase).IsEqualTo(GamePhase.Night);
		await Assert.That(state.CurrentSongId).IsEqualTo(SongId.From("/music/d.mp3"));
	}

	[Test]
	public async Task Select_SetsTheRequestedSongAsCurrent()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playback/select", JsonBody("""{"phase":"Day","index":1}"""));

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
		var state = await ReadPlayback(client);
		await Assert.That(state.CurrentSongId).IsEqualTo(SecondDaySongId);
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
	public async Task SetGain_UpdatesThePhaseGain()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/playback/set-gain", JsonBody("""{"phase":"Day","value":0.5}"""));

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
		var state = await ReadPlayback(client);
		await Assert.That(state.Playlists.Day.Gain).IsEqualTo(0.5);
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
	public async Task Audio_ServesTheSongBytes_ForKnownSong()
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
			await Assert.That(Convert.ToHexString(await response.Content.ReadAsByteArrayAsync())).IsEqualTo("494433");
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
