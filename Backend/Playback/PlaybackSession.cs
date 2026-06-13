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
			state = PlaybackTimeline.Play(state, songId, Now());
			Broadcast(CurrentSnapshot());
		}
	}

	public void Resume()
	{
		lock (gate)
		{
			state = PlaybackTimeline.Resume(state, Now());
			Broadcast(CurrentSnapshot());
		}
	}

	public void Pause()
	{
		lock (gate)
		{
			state = PlaybackTimeline.Pause(state, Now());
			Broadcast(CurrentSnapshot());
		}
	}

	public bool HasCurrentSong
	{
		get
		{
			lock (gate)
			{
				return state.CurrentSongId is not null;
			}
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

	private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	private void Broadcast(StateSnapshot snapshot)
	{
		foreach (var subscriber in subscribers)
		{
			subscriber.Writer.TryWrite(snapshot);
		}
	}

	private StateSnapshot CurrentSnapshot() => SnapshotMapper.ToSnapshot(state, library.Entries);
}
