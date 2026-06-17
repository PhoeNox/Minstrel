namespace Core.Tests.Playlist;

using Core.Playlist;

public class IndexOfSong
{
	private static PlaylistEntry[] Songs(params string[] ids) =>
		ids.Select(id => Fixtures.Entry(id, 10)).ToArray();

	[Test]
	public async Task ReturnsTheIndexOfTheMatchingSong()
	{
		var entries = Songs("a", "b", "c");

		await Assert.That(Playlists.IndexOfSong(entries, "b")).IsEqualTo(1);
	}

	[Test]
	public async Task ReturnsNullWhenNoEntryMatches()
	{
		var entries = Songs("a", "b");

		await Assert.That(Playlists.IndexOfSong(entries, "z")).IsNull();
	}

	[Test]
	public async Task ReturnsTheFirstIndexWhenTheIdAppearsMoreThanOnce()
	{
		var entries = Songs("a", "b", "b");

		await Assert.That(Playlists.IndexOfSong(entries, "b")).IsEqualTo(1);
	}
}
