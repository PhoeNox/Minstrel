namespace Backend.Tests.Playback;

using Core;
using FileSystem;
using FileSystem.Tests;
using Features.Session;

public class Persistence
{
	private static LiveSession Build(FakeFileSystem fileSystem)
	{
		var provider = new SongPoolProvider(fileSystem);
		var pool = provider.SongPool;
		return new LiveSession(pool, fileSystem);
	}

	[Test]
	public async Task SavesReorderedPlaylistPathsAfterMove()
	{
		var fileSystem = new FakeFileSystem(
				day: [TestSongs.At("a.mp3"), TestSongs.At("b.mp3")],
				night: []);
		var session = Build(fileSystem);

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
		var poolProvider = new SongPoolProvider(fileSystem);
		var pool = poolProvider.SongPool;
		var session = new LiveSession(pool, fileSystem);
		var added = pool.All.Single(entry => entry.Song.Path == "c.mp3");

		session.Add(GamePhase.Day, added.Id);

		await Assert.That(fileSystem.Saved(GamePhase.Day)).IsEquivalentTo(["a.mp3", "c.mp3"]);
	}
}
