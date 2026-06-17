namespace Backend.Playback;

using System.Threading.Channels;
using Backend.Contracts;
using Backend.Library;
using Backend.Timer;
using Core;

public sealed class PlaybackSession(SongLibrary library)
{
	private readonly Lock gate = new();
	private readonly List<Channel<SessionEvent>> subscribers = [];
	private TimelineState state = TimelineState.Idle with
	{
			Day = ToPlaylist(library.Day),
			Night = ToPlaylist(library.Night),
	};
	private TimerAnchor timer = TimerAnchor.Idle;

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

	public void Play(string songId)
	{
		lock (gate)
		{
			var index = Array.FindIndex(state.ActivePlaylist.Songs, song => song.Id == songId);
			if (index < 0)
				return;

			state = PlaybackTimeline.SelectSong(state, state.ActivePhase, index, Now());
			Broadcast(CurrentSnapshot());
		}
	}

	public bool PlayCurrent()
	{
		lock (gate)
		{
			if (state.ActivePlaylist.CurrentIndex is not { } index)
				return false;

			state = PlaybackTimeline.SelectSong(state, state.ActivePhase, index, Now());
			Broadcast(CurrentSnapshot());
			return true;
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

	public void SelectSong(GamePhase phase, int index)
	{
		lock (gate)
		{
			state = PlaybackTimeline.SelectSong(state, phase, index, Now());
			Broadcast(CurrentSnapshot());
		}
	}

	public void SetGain(GamePhase phase, double value)
	{
		lock (gate)
		{
			state = PlaybackTimeline.SetGain(state, phase, value);
			Broadcast(CurrentSnapshot());
		}
	}

	public void SwitchPhase()
	{
		lock (gate)
		{
			state = PlaybackTimeline.SwitchPhase(state, Now());
			Broadcast(CurrentSnapshot());
		}
	}

	public void AddSong(GamePhase phase, string songId)
	{
		lock (gate)
		{
			state = PlaybackTimeline.AddSong(state, phase, songId, library.Length(songId));
			var playlist = phase == GamePhase.Day ? state.Day : state.Night;
			library.SaveOrder(phase, playlist.Songs.Select(song => song.Id).ToArray());
			Broadcast(CurrentSnapshot());
		}
	}

	public void RemoveSong(GamePhase phase, int index)
	{
		lock (gate)
		{
			state = PlaybackTimeline.RemoveSong(state, phase, index, Now());
			var playlist = phase == GamePhase.Day ? state.Day : state.Night;
			library.SaveOrder(phase, playlist.Songs.Select(song => song.Id).ToArray());
			Broadcast(CurrentSnapshot());
		}
	}

	public void Shuffle(GamePhase phase)
	{
		lock (gate)
		{
			state = PlaybackTimeline.Shuffle(state, phase, Random.Shared);
			var playlist = phase == GamePhase.Day ? state.Day : state.Night;
			library.SaveOrder(phase, playlist.Songs.Select(song => song.Id).ToArray());
			Broadcast(CurrentSnapshot());
		}
	}

	public void MoveSong(GamePhase phase, int oldIndex, int newIndex)
	{
		lock (gate)
		{
			state = PlaybackTimeline.MoveSong(state, phase, oldIndex, newIndex);
			var playlist = phase == GamePhase.Day ? state.Day : state.Night;
			library.SaveOrder(phase, playlist.Songs.Select(song => song.Id).ToArray());
			Broadcast(CurrentSnapshot());
		}
	}

	public void StartTimer(TimeSpan duration)
	{
		lock (gate)
		{
			timer = CountdownTimer.Start(duration, Now());
			Broadcast(CurrentSnapshot());
		}
	}

	public void StopTimer()
	{
		lock (gate)
		{
			timer = TimerAnchor.Idle;
			Broadcast(CurrentSnapshot());
		}
	}

	public void Tick()
	{
		lock (gate)
		{
			var now = Now();
			var previous = state;
			state = PlaybackTimeline.Tick(state, now);
			var expired = CountdownTimer.HasExpired(timer, now);
			if (expired)
				timer = TimerAnchor.Idle;

			if (PlaybackTimeline.PlaybackJumped(previous, state, now) || expired)
				Broadcast(CurrentSnapshot());
			if (expired)
				Publish(new GongEvent());
		}
	}

	public Channel<SessionEvent> Subscribe()
	{
		var channel = Channel.CreateUnbounded<SessionEvent>();
		lock (gate)
		{
			subscribers.Add(channel);
			channel.Writer.TryWrite(new SnapshotEvent(CurrentSnapshot()));
		}

		return channel;
	}

	public void Unsubscribe(Channel<SessionEvent> channel)
	{
		lock (gate)
		{
			subscribers.Remove(channel);
		}

		channel.Writer.TryComplete();
	}

	private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	private static TimelinePlaylist ToPlaylist(IReadOnlyList<LibraryEntry> entries) =>
		new(
			entries.Select(entry => new TimelineSong(entry.Id, entry.Song.Length.TotalSeconds)).ToArray(),
			entries.Count > 0 ? 0 : null);

	private void Broadcast(StateSnapshot snapshot) => Publish(new SnapshotEvent(snapshot));

	private void Publish(SessionEvent message)
	{
		foreach (var subscriber in subscribers)
		{
			subscriber.Writer.TryWrite(message);
		}
	}

	private StateSnapshot CurrentSnapshot() => SnapshotMapper.ToSnapshot(state, timer, library);
}
