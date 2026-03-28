namespace Core.Tests;

public class SongExtensionsTests
{
	private static Song Song(string title) => new("Path", title, "Artist", "Album", TimeSpan.FromMinutes(1));

	private readonly Song song1 = Song("Song1");
	private readonly Song song2 = Song("Song2");
	private readonly Song song3 = Song("Song3");

	[Test]
	public async Task WithSongAdded_AppendsSongToEnd()
	{
		Song[] songs = [song1, song2];
		await Assert.That(songs.WithSongAdded(song3)).IsEquivalentTo([song1, song2, song3]);
	}

	[Test]
	public async Task WithSongRemoved_RemovesMatchingSong()
	{
		Song[] songs = [song1, song2, song3];
		await Assert.That(songs.WithSongRemoved(song2)).IsEquivalentTo([song1, song3]);
	}

	[Test]
	public async Task WithSongMoved_MovesForward()
	{
		Song[] songs = [song1, song2, song3];
		await Assert.That(songs.WithSongMoved(0, 2)).IsEquivalentTo([song2, song3, song1]);
	}

	[Test]
	public async Task WithSongMoved_MovesBackward()
	{
		Song[] songs = [song1, song2, song3];
		await Assert.That(songs.WithSongMoved(2, 0)).IsEquivalentTo([song3, song1, song2]);
	}

	[Test]
	public async Task WithSongMoved_MoveToEnd_WhenNewIndexExceedsBounds()
	{
		Song[] songs = [song1, song2, song3];
		await Assert.That(songs.WithSongMoved(0, 3)).IsEquivalentTo([song2, song3, song1]);
	}
}
