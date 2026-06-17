namespace Core.Tests.Library;

using Core;
using Core.Library;

public class SongPoolTests
{
	private static Song Song(string path)
		=> new(path, Title: path, Artist: "Artist", Album: "Album", Length: TimeSpan.FromMinutes(3));

	[Test]
	public async Task AssignsAStableIdAcrossReloads()
	{
		var first = SongPool.From([Song("a.mp3")], [], []);
		var second = SongPool.From([Song("a.mp3")], [], []);

		await Assert.That(second.Day[0].Id).IsEqualTo(first.Day[0].Id);
	}

	[Test]
	public async Task AssignsDistinctIdsToDistinctPaths()
	{
		var pool = SongPool.From([Song("a.mp3"), Song("b.mp3")], [], []);

		await Assert.That(pool.Day[0].Id).IsNotEqualTo(pool.Day[1].Id);
	}

	[Test]
	public async Task RoundTripsSongIdToSong()
	{
		var pool = SongPool.From([Song("a.mp3")], [Song("b.mp3")], []);

		var ok = pool.EntriesById.TryGetValue(pool.Night[0].Id, out var entry);

		await Assert.That(ok).IsTrue();
		await Assert.That(entry?.Song.Path).IsEqualTo("b.mp3");
	}
}
