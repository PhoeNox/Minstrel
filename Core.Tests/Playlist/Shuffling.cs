namespace Core.Tests.Playlist;

using Core.Playlist;

public class Shuffling
{
	private static PlaylistEntry[] Songs(params string[] ids) =>
		ids.Select(id => Fixtures.Entry(id, 100)).ToArray();

	[Test]
	public async Task KeepsEveryEntry()
	{
		var entries = Songs("a", "b", "c", "d", "e");

		var shuffled = Playlists.Shuffle(entries, pinnedIndex: null, new Random(1));

		var entryIds = shuffled.Select(entry => entry.Id).ToArray();
		await Assert.That(entryIds).HasCount(5)
				.And.Contains("a")
				.And.Contains("b")
				.And.Contains("c")
				.And.Contains("d")
				.And.Contains("e");
	}

	[Test]
	public async Task IsDeterministicForASeededRandom()
	{
		var entries = Songs("a", "b", "c", "d", "e");

		var first = Playlists.Shuffle(entries, pinnedIndex: null, new Random(42));
		var second = Playlists.Shuffle(entries, pinnedIndex: null, new Random(42));

		var firstOrder = string.Join(",", first.Select(entry => entry.Id));
		var secondOrder = string.Join(",", second.Select(entry => entry.Id));
		await Assert.That(firstOrder).IsEqualTo(secondOrder);
	}

	[Test]
	public async Task PlacesThePinnedEntryFirst()
	{
		var entries = Songs("a", "b", "c", "d", "e");

		var shuffled = Playlists.Shuffle(entries, pinnedIndex: 2, new Random(1));

		await Assert.That(shuffled[0].Id).IsEqualTo("c");
	}

	[Test]
	public async Task KeepsEveryEntryWhenPinning()
	{
		var entries = Songs("a", "b", "c", "d", "e");

		var shuffled = Playlists.Shuffle(entries, pinnedIndex: 4, new Random(3));

		var entryIds = shuffled.Select(entry => entry.Id).ToArray();
		await Assert.That(entryIds).HasCount(5)
				.And.Contains("a")
				.And.Contains("b")
				.And.Contains("c")
				.And.Contains("d")
				.And.Contains("e");
	}
}
