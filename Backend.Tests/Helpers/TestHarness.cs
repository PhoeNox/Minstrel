namespace Backend.Tests.Helpers;

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Api;
using Core;
using FileSystem.Tests;

// Shared rig for the endpoint integration tests: seeded file systems, the JSON contract
// the Backend speaks, request builders, and response projections handed to Verify.
public static class TestHarness
{
	public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
	{
		Converters = { new JsonStringEnumConverter() },
	};

	// A two-song Day playlist, empty Night, plus one further song on disk (id below) that
	// is in the library but not yet on any playlist — the candidate for /playlist/add.
	public static FakeFileSystem SeededLibrary() => new(
		day: [TestSongs.At("a.mp3"), TestSongs.At("b.mp3")],
		night: [],
		alsoOnDisk: [TestSongs.At("c.mp3")]);

	public static FakeFileSystem EmptyLibrary() => new(day: [], night: []);

	public static readonly string AddableSongId = SongId.From("/music/c.mp3");
	public static readonly string FirstDaySongId = SongId.From("/music/a.mp3");
	public static readonly string SecondDaySongId = SongId.From("/music/b.mp3");

	public static StringContent JsonBody(string raw) => new(raw, Encoding.UTF8, "application/json");

	public static StringContent NullBody() => JsonBody("null");

	// The current state as a subscriber sees it: the first SSE frame each stream emits on
	// subscribe is the live snapshot, so this reads back the effect of a preceding command.
	public static async Task<StateSnapshot> ReadPlayback(HttpClient client)
	{
		var data = await Sse.FirstData(client, "/playback/sse");
		return JsonSerializer.Deserialize<StateSnapshot>(data!, Json)!;
	}

	public static async Task<TimerDto?> ReadTimer(HttpClient client)
	{
		var data = await Sse.FirstData(client, "/timer/sse");
		return JsonSerializer.Deserialize<TimerDto>(data!, Json);
	}

	public static async Task<object> StatusAndJson<T>(HttpResponseMessage response)
	{
		var body = await response.Content.ReadFromJsonAsync<T>(Json);
		return new
		{
			Status = (int)response.StatusCode,
			Body = body,
		};
	}
}
