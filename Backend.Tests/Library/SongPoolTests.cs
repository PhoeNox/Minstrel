namespace Backend.Tests.Library;

using Features.Library;

public class SongPoolTests
{
	private static SongPool Build(FakeFileSystem fileSystem) => new(fileSystem);

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
	public async Task ExtractsTitleArtistAndLengthMetadata()
	{
		var fileSystem = new FakeFileSystem(
			day: [TestSongs.At("a.mp3", TimeSpan.FromSeconds(42))],
			night: []);

		var pool = Build(fileSystem);
		var dto = pool.ToSongDto(pool.Day[0].Id);

		await Assert.That(dto.Title).IsEqualTo("Title a.mp3");
		await Assert.That(dto.Artist).IsEqualTo("Artist a.mp3");
		await Assert.That(dto.Length).IsEqualTo(42);
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

		var orphan = pool.All.Single(entry => entry.Song.Path == "orphan.mp3");
		var ok = pool.TryGetPath(orphan.Id, out var path);

		await Assert.That(ok).IsTrue();
		await Assert.That(path).IsEqualTo("orphan.mp3");
	}

	[Test]
	public async Task RoundTripsSongIdToPath()
	{
		var pool = Build(new FakeFileSystem([TestSongs.At("a.mp3")], [TestSongs.At("b.mp3")]));

		var ok = pool.TryGetPath(pool.Night[0].Id, out var path);

		await Assert.That(ok).IsTrue();
		await Assert.That(path).IsEqualTo("b.mp3");
	}

	[Test]
	public async Task ReturnsFalseForAnUnknownSongId()
	{
		var pool = Build(new FakeFileSystem([TestSongs.At("a.mp3")], []));

		var ok = pool.TryGetPath("unknown", out var path);

		await Assert.That(ok).IsFalse();
		await Assert.That(path).IsEqualTo(string.Empty);
	}
}
