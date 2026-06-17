namespace Core.Tests.Playlist;

using Core.Playlist;

public class Removing
{
	private static PlaylistEntry[] Songs(params string[] ids) =>
		ids.Select(id => Fixtures.Entry(id, 10)).ToArray();

	[Test]
	public async Task DropsTheEntryAtTheGivenIndex()
	{
		var entries = Songs("a", "b", "c");

		var remaining = Playlists.RemoveAt(entries, index: 1);

		await Assert.That(remaining.Select(entry => entry.Id)).IsEquivalentTo(new[] { "a", "c" });
	}

	[Test]
	public async Task IgnoresAnOutOfRangeIndex()
	{
		var entries = Songs("a", "b");

		var remaining = Playlists.RemoveAt(entries, index: 5);

		await Assert.That(remaining.Select(entry => entry.Id)).IsEquivalentTo(new[] { "a", "b" });
	}
}
