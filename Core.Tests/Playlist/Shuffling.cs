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

		var (shuffled, _) = Playlists.Shuffle(entries, new Random(1));

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

		var (first, _) = Playlists.Shuffle(entries, new Random(42));
		var (second, _) = Playlists.Shuffle(entries, new Random(42));

		var firstOrder = string.Join(",", first.Select(entry => entry.Id));
		var secondOrder = string.Join(",", second.Select(entry => entry.Id));
		await Assert.That(firstOrder).IsEqualTo(secondOrder);
	}
}
