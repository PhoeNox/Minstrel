namespace Backend.Tests.Library;

using Core.Library;
using Features.Library;

public class SongPoolProviderTests
{
	private static SongPool Build(FakeFileSystem fileSystem)
	{
		var provider = new SongPoolProvider(fileSystem);
		return provider.SongPool;
	}

	[Test]
	public async Task LoadsBothPlaylistsFromTheFileSystem()
	{
		var fileSystem = new FakeFileSystem(
			day: [TestSongs.At("a.mp3")],
			night: [TestSongs.At("b.mp3"), TestSongs.At("c.mp3")]);

		var pool = Build(fileSystem);

		await Assert.That(pool.Day).Count().IsEqualTo(1);
		await Assert.That(pool.Night).Count().IsEqualTo(2);
	}

	[Test]
	public async Task AssignsAStableIdAcrossReloads()
	{
		var first = Build(new FakeFileSystem([TestSongs.At("a.mp3")], []));
		var second = Build(new FakeFileSystem([TestSongs.At("a.mp3")], []));

		await Assert.That(second.Day[0].Id).IsEqualTo(first.Day[0].Id);
	}

	[Test]
	public async Task AssignsDistinctIdsToDistinctPaths()
	{
		var pool = Build(new FakeFileSystem([TestSongs.At("a.mp3"), TestSongs.At("b.mp3")], []));

		await Assert.That(pool.Day[0].Id).IsNotEqualTo(pool.Day[1].Id);
	}

	[Test]
	public async Task IndexesSongsOnDiskAbsentFromBothPlaylists()
	{
		var fileSystem = new FakeFileSystem(
			day: [TestSongs.At("a.mp3")],
			night: [],
			alsoOnDisk: [TestSongs.At("orphan.mp3")]);
		var pool = Build(fileSystem);

		var orphan = pool.All.Single(e => e.Song.Path == "orphan.mp3");
		var ok = pool.EntriesById.TryGetValue(orphan.Id, out var entry);

		await Assert.That(ok).IsTrue();
		await Assert.That(entry?.Song.Path).IsEqualTo("orphan.mp3");
	}

	[Test]
	public async Task RoundTripsSongIdToPath()
	{
		var pool = Build(new FakeFileSystem([TestSongs.At("a.mp3")], [TestSongs.At("b.mp3")]));

		var ok = pool.EntriesById.TryGetValue(pool.Night[0].Id, out var entry);

		await Assert.That(ok).IsTrue();
		await Assert.That(entry?.Song.Path).IsEqualTo("b.mp3");
	}
}
