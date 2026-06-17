namespace Backend.Tests.Api;

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

	public static StringContent JsonBody(string raw) => new(raw, Encoding.UTF8, "application/json");

	public static StringContent NullBody() => JsonBody("null");

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
