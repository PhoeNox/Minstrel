namespace Backend.Playback;

using System.Threading.Channels;
using Backend.Contracts;
using Backend.Library;

public sealed class PlaybackSession(SongLibrary library)
{
	private readonly Lock gate = new();
	private readonly List<Channel<StateSnapshot>> subscribers = [];
	private TimelineState state = TimelineState.Idle;

	public void Play(string songId)
	{
		lock (gate)
		{
			state = PlaybackTimeline.Play(state, songId);
			Broadcast(CurrentSnapshot());
		}
	}

	public Channel<StateSnapshot> Subscribe()
	{
		var channel = Channel.CreateUnbounded<StateSnapshot>();
		lock (gate)
		{
			subscribers.Add(channel);
			channel.Writer.TryWrite(CurrentSnapshot());
		}

		return channel;
	}

	public void Unsubscribe(Channel<StateSnapshot> channel)
	{
		lock (gate)
		{
			subscribers.Remove(channel);
		}

		channel.Writer.TryComplete();
	}

	private void Broadcast(StateSnapshot snapshot)
	{
		foreach (var subscriber in subscribers)
		{
			subscriber.Writer.TryWrite(snapshot);
		}
	}

	private StateSnapshot CurrentSnapshot() => SnapshotMapper.ToSnapshot(state, library.Entries);
}
