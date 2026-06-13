namespace Backend.Tests.Playback;

using Backend.Playback;

public class DerivePosition
{
	[Test]
	public async Task AdvancesWithElapsedTimeWhilePlaying()
	{
		var anchor = new PositionAnchor("song-1", Offset: 10, AnchorTimestamp: 2000, IsPlaying: true);

		var position = PlaybackTimeline.DerivePosition(anchor, now: 4500);

		await Assert.That(position).IsEqualTo(12.5);
	}

	[Test]
	public async Task StaysAtTheOffsetWhilePaused()
	{
		var anchor = new PositionAnchor("song-1", Offset: 10, AnchorTimestamp: 2000, IsPlaying: false);

		var position = PlaybackTimeline.DerivePosition(anchor, now: 4500);

		await Assert.That(position).IsEqualTo(10);
	}
}
