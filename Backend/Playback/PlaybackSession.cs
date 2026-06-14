namespace Backend.Playback;

using System.Threading.Channels;
using Backend.Contracts;
using Backend.Library;
using Backend.Timer;
using Core;

public sealed class PlaybackSession
{
	private readonly SongLibrary library;
	private readonly Lock gate = new();
	private readonly List<Channel<SessionEvent>> subscribers = [];
	private TimelineState state;
	private TimerAnchor timer = TimerAnchor.Idle;

	public PlaybackSession(SongLibrary library)
	{
		this.library = library;
		state = TimelineState.Idle with
		{
			Day = ToPlaylist(library.Day),
			Night = ToPlaylist(library.Night),
		};
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

	public void Play(string songId)
	{
		lock (gate)
		{
			state = PlaybackTimeline.SelectSong(state, state.ActivePhase, songId, Now());
			Broadcast(CurrentSnapshot());
		}
	}

	public bool PlayCurrent()
	{
		lock (gate)
		{
			if (state.ActivePlaylist.CurrentSongId is not { } songId)
				return false;

			state = PlaybackTimeline.SelectSong(state, state.ActivePhase, songId, Now());
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

	public void SelectSong(GamePhase phase, string songId)
	{
		lock (gate)
		{
			state = PlaybackTimeline.SelectSong(state, phase, songId, Now());
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

	public void StartTimer(double durationSeconds)
	{
		lock (gate)
		{
			timer = CountdownTimer.Start(durationSeconds, Now());
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

			if (state.CurrentSongId != previous.CurrentSongId || expired)
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
			entries.FirstOrDefault()?.Id);

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
