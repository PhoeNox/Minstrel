namespace Backend.Tests;

using System.Globalization;
using System.Threading.Channels;
using Backend.Sessions.Playback;
using Core;
using Core.Library;
using FileSystem;
using FileSystem.Tests;

public class PlaybackSessionBackpressureTests
{
	[Test]
	public async Task Subscriber_DropsIntermediateSnapshots_ButKeepsTheNewest_WhenItStopsDraining()
	{
		var session = NewSession(new FakeFileSystem(
			day: [TestSongs.At("a.mp3")],
			night: []));
		var channel = session.Subscribe();

		const int broadcasts = 50;
		for (var i = 1; i <= broadcasts; i++)
			session.SetGain(GamePhase.Day, i / 100.0);

		var drained = Drain(channel);

		await Assert.That(drained.Count).IsLessThan(broadcasts);
		await Assert.That(Gain(drained[^1])).IsEqualTo(broadcasts / 100.0);
	}

	private static double Gain(PlaybackEvent message)
		=> double.Parse(((SnapshotEvent)message).Frame, CultureInfo.InvariantCulture);

	private static List<PlaybackEvent> Drain(Channel<PlaybackEvent> channel)
	{
		var messages = new List<PlaybackEvent>();
		while (channel.Reader.TryRead(out var message))
			messages.Add(message);
		return messages;
	}

	private static PlaybackSession NewSession(FakeFileSystem fileSystem)
	{
		var playlists = new PlaylistProvider(fileSystem);
		var (day, night) = playlists.LoadPlaylists();
		var pool = SongPool.From(day, night, day.Concat(night).ToArray());
		return new PlaybackSession(pool, playlists,
			(playback, _) => playback.Day.Gain.ToString(CultureInfo.InvariantCulture));
	}
}
