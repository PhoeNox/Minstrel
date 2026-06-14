namespace Backend.Tests.Library;

using Backend.Library;
using Core;

public class SongLibraryTests
{
	private static SongLibrary Build(FakeFileSystem fileSystem) => new(fileSystem);

	[Test]
	public async Task LoadsBothPlaylistsFromTheFileSystem()
	{
		var fileSystem = new FakeFileSystem(
			day: [TestSongs.At("a.mp3")],
			night: [TestSongs.At("b.mp3"), TestSongs.At("c.mp3")]);

		var library = Build(fileSystem);

		await Assert.That(library.Day).Count().IsEqualTo(1);
		await Assert.That(library.Night).Count().IsEqualTo(2);
	}

	[Test]
	public async Task ExtractsTitleArtistAndLengthMetadata()
	{
		var fileSystem = new FakeFileSystem(
			day: [TestSongs.At("a.mp3", TimeSpan.FromSeconds(42))],
			night: []);

		var library = Build(fileSystem);
		var dto = library.ToSongDto(library.Day[0].Id);

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
		var library = Build(new FakeFileSystem([TestSongs.At("a.mp3"), TestSongs.At("b.mp3")], []));

		await Assert.That(library.Day[0].Id).IsNotEqualTo(library.Day[1].Id);
	}

	[Test]
	public async Task IndexesSongsOnDiskAbsentFromBothPlaylists()
	{
		var fileSystem = new FakeFileSystem(
			day: [TestSongs.At("a.mp3")],
			night: [],
			alsoOnDisk: [TestSongs.At("orphan.mp3")]);
		var library = Build(fileSystem);

		var orphan = library.All.Single(entry => entry.Song.Path == "orphan.mp3");
		var ok = library.TryGetPath(orphan.Id, out var path);

		await Assert.That(ok).IsTrue();
		await Assert.That(path).IsEqualTo("orphan.mp3");
	}

	[Test]
	public async Task RoundTripsSongIdToPath()
	{
		var library = Build(new FakeFileSystem([TestSongs.At("a.mp3")], [TestSongs.At("b.mp3")]));

		var ok = library.TryGetPath(library.Night[0].Id, out var path);

		await Assert.That(ok).IsTrue();
		await Assert.That(path).IsEqualTo("b.mp3");
	}

	[Test]
	public async Task ReturnsFalseForAnUnknownSongId()
	{
		var library = Build(new FakeFileSystem([TestSongs.At("a.mp3")], []));

		var ok = library.TryGetPath("unknown", out var path);

		await Assert.That(ok).IsFalse();
		await Assert.That(path).IsEqualTo(string.Empty);
	}

	[Test]
	public async Task SavesPlaylistOrderAsResolvedSongPaths()
	{
		var fileSystem = new FakeFileSystem(
			day: [TestSongs.At("a.mp3"), TestSongs.At("b.mp3")],
			night: []);
		var library = Build(fileSystem);

		library.SaveOrder(GamePhase.Day, [library.Day[1].Id, library.Day[0].Id]);

		var saved = fileSystem.Saved(GamePhase.Day).Select(song => song.Path).ToArray();
		await Assert.That(saved).IsEquivalentTo(new[] { "b.mp3", "a.mp3" });
	}
}
