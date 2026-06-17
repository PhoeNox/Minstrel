namespace FileSystem.Tests;

using Core;

public class PlaylistProviderTests
{
	[Test]
	public async Task LoadsBothPlaylistsFromTheFileSystem()
	{
		var fileSystem = new FakeFileSystem(
			day: [TestSongs.At("a.mp3")],
			night: [TestSongs.At("b.mp3"), TestSongs.At("c.mp3")]);

		var (day, night) = new PlaylistProvider(fileSystem).LoadPlaylists();

		await Assert.That(day).Count().IsEqualTo(1);
		await Assert.That(night).Count().IsEqualTo(2);
	}

	[Test]
	public async Task SavesPathsRelativeToTheMusicDirectory()
	{
		var fileSystem = new FakeFileSystem(
			day: [TestSongs.At("a.mp3"), TestSongs.At("b.mp3")],
			night: []);

		var playlistProvider = new PlaylistProvider(fileSystem);
		playlistProvider.Save(GamePhase.Day, ["/music/b.mp3", "/music/a.mp3"]);

		var savedPaths = fileSystem.Saved(GamePhase.Day);
		await Assert.That(savedPaths).IsEquivalentTo(["b.mp3", "a.mp3"]);
	}
}
