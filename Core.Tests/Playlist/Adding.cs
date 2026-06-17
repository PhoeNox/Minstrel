namespace Core.Tests.Playlist;

using Core.Playlist;

public class Adding
{
	[Test]
	public async Task AppendsTheEntryToTheEndOfThePlaylist()
	{
		var entries = new[] { Fixtures.Entry("a", 10), Fixtures.Entry("b", 10) };

		var added = Playlists.Add(entries, Fixtures.Entry("c", 20));

		await Assert.That(added.Select(entry => entry.Id)).IsEquivalentTo(new[] { "a", "b", "c" });
		await Assert.That(added[^1].Length).IsEqualTo(20);
	}

	[Test]
	public async Task AllowsAddingTheSameEntryTwice()
	{
		var entries = new[] { Fixtures.Entry("a", 10) };

		var added = Playlists.Add(entries, Fixtures.Entry("a", 10));

		await Assert.That(added.Select(entry => entry.Id)).IsEquivalentTo(new[] { "a", "a" });
	}
}
