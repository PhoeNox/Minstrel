namespace Backend.Tests;

using Backend.Sessions.Playback;
using Core.Library;
using FileSystem;
using FileSystem.Tests;

public class PlaybackSessionTickTests
{
	[Test]
	public async Task Tick_DoesNothing_WhileIdle()
	{
		var session = NewSession(new FakeFileSystem(
			day: [TestSongs.At("a.mp3"), TestSongs.At("b.mp3")],
			night: []));
		var channel = session.Subscribe();
		channel.Reader.TryRead(out _);

		session.Tick();

		await Assert.That(channel.Reader.TryRead(out _)).IsFalse();
	}

	[Test]
	public async Task Tick_Broadcasts_WhenPlayingSongEnds()
	{
		var session = NewSession(new FakeFileSystem(
			day: [TestSongs.At("a.mp3", TimeSpan.Zero), TestSongs.At("b.mp3")],
			night: []));
		session.PlayCurrent();
		var channel = session.Subscribe();
		channel.Reader.TryRead(out _);

		session.Tick();

		await Assert.That(channel.Reader.TryRead(out _)).IsTrue();
	}

	private static PlaybackSession NewSession(FakeFileSystem fileSystem)
	{
		var playlists = new PlaylistProvider(fileSystem);
		var (day, night) = playlists.LoadPlaylists();
		var pool = SongPool.From(day, night, day.Concat(night).ToArray());
		return new PlaybackSession(pool, playlists);
	}
}
