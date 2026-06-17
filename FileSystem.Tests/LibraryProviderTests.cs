namespace FileSystem.Tests;

public class LibraryProviderTests
{
	[Test]
	public async Task LoadsEverySongOnDisk()
	{
		var fileSystem = new FakeFileSystem(
			day: [TestSongs.At("a.mp3")],
			night: [TestSongs.At("b.mp3"), TestSongs.At("c.mp3")]);

		var library = new LibraryProvider(fileSystem).LoadLibrary();

		await Assert.That(library).Count().IsEqualTo(3);
	}

	[Test]
	public async Task IncludesSongsAbsentFromBothPlaylists()
	{
		var fileSystem = new FakeFileSystem(
			day: [TestSongs.At("a.mp3")],
			night: [],
			alsoOnDisk: [TestSongs.At("orphan.mp3")]);

		var library = new LibraryProvider(fileSystem).LoadLibrary();

		var loadedSongPaths = library.Select(song => song.Path);
		await Assert.That(loadedSongPaths).Contains("/music/orphan.mp3");
	}
}
