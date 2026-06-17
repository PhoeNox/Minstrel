namespace Backend.Tests.Playback;

using Core;
using Core.Library;
using FileSystem;
using FileSystem.Tests;
using Sessions;

public class Persistence
{
	private static (LiveSession Session, SongPool Pool) Build(FakeFileSystem fileSystem)
	{
		var (day, night) = new PlaylistProvider(fileSystem).LoadPlaylists();
		var all = new LibraryProvider(fileSystem).LoadLibrary();
		var pool = SongPool.From(day, night, all);
		return (new LiveSession(pool, new PlaylistProvider(fileSystem)), pool);
	}

	[Test]
	public async Task SavesReorderedPlaylistPathsAfterMove()
	{
		var fileSystem = new FakeFileSystem(
				day: [TestSongs.At("a.mp3"), TestSongs.At("b.mp3")],
				night: []);
		var (session, _) = Build(fileSystem);

		session.Move(GamePhase.Day, oldIndex: 0, newIndex: 1);

		await Assert.That(fileSystem.Saved(GamePhase.Day)).IsEquivalentTo(["b.mp3", "a.mp3"]);
	}

	[Test]
	public async Task SavesAppendedPathWhenAddingASong()
	{
		var fileSystem = new FakeFileSystem(
				day: [TestSongs.At("a.mp3")],
				night: [],
				alsoOnDisk: [TestSongs.At("c.mp3")]);
		var (session, pool) = Build(fileSystem);
		var added = pool.All.Single(entry => entry.Song.Path == "/music/c.mp3");

		session.Add(GamePhase.Day, added.Id);

		await Assert.That(fileSystem.Saved(GamePhase.Day)).IsEquivalentTo(["a.mp3", "c.mp3"]);
	}
}
