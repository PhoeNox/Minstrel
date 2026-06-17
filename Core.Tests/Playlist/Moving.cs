namespace Core.Tests.Playlist;

using Core.Playlist;

public class Moving
{
	private static PlaylistEntry[] Songs(params string[] ids) =>
		ids.Select(id => Fixtures.Entry(id, 10)).ToArray();

	[Test]
	public async Task ReordersSongsWithinThePlaylist()
	{
		var entries = Songs("a", "b", "c");

		var moved = Playlists.Move(entries, oldIndex: 0, newIndex: 2);

		await Assert.That(moved.Select(entry => entry.Id)).IsEquivalentTo(new[] { "b", "c", "a" });
	}

	[Test]
	public async Task IgnoresAnOutOfRangeSourceIndex()
	{
		var entries = Songs("a", "b");

		var moved = Playlists.Move(entries, oldIndex: 5, newIndex: 0);

		await Assert.That(moved.Select(entry => entry.Id)).IsEquivalentTo(new[] { "a", "b" });
	}
}
