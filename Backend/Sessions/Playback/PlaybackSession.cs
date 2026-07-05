namespace Backend.Sessions.Playback;

using System.Threading.Channels;
using Core;
using Core.Library;
using Core.Playback;
using Core.Playlist;
using FileSystem;

public sealed class PlaybackSession(
	SongPool pool,
	PlaylistProvider playlistStore,
	Func<PlaybackState, PlaylistBook, string> renderFrame)
{
	// A half-open subscriber stalls its reader; bounding with DropOldest caps the
	// backlog and lets it resync from the newest snapshot once it drains, since the
	// payload is a full snapshot rather than a delta.
	private const int SnapshotBacklog = 8;

	private readonly Lock gate = new();
	private readonly SseBroadcaster<PlaybackEvent> broadcaster = new(CreateBoundedChannel);

	private PlaylistBook playlists = new(ToEntries(pool.Day), ToEntries(pool.Night));

	private PlaybackState playback = PlaybackState.Idle with
	{
			Day = InitialPhase(pool.Day.Length),
			Night = InitialPhase(pool.Night.Length),
	};

	public bool HasCurrentSong
	{
		get
		{
			lock (gate)
			{
				return playback.CurrentSongId is not null;
			}
		}
	}

	public bool Play(string songId)
	{
		lock (gate)
		{
			var entries = playlists.Entries(playback.ActivePhase);
			if (Playlists.IndexOfSong(entries, songId) is not { } index)
				return false;

			playback = PlaybackTimeline.Select(playback, playback.ActivePhase, index,
					ToTrack(entries[index]), Now());
			Broadcast();
			return true;
		}
	}

	public bool PlayCurrent()
	{
		lock (gate)
		{
			if (playback.ActiveCursor is not { } index)
				return false;

			var track = ToTrack(playlists.At(playback.ActivePhase, index));
			playback = PlaybackTimeline.Select(playback, playback.ActivePhase, index, track, Now());
			Broadcast();
			return true;
		}
	}

	public void Resume()
	{
		lock (gate)
		{
			playback = PlaybackTimeline.Resume(playback, Now());
			Broadcast();
		}
	}

	public void Pause()
	{
		lock (gate)
		{
			playback = PlaybackTimeline.Pause(playback, Now());
			Broadcast();
		}
	}

	public bool Select(GamePhase phase, int index)
	{
		lock (gate)
		{
			if (!InRange(phase, index))
				return false;

			var track = ToTrack(playlists.At(phase, index));
			playback = PlaybackTimeline.Select(playback, phase, index, track, Now());
			Broadcast();
			return true;
		}
	}

	public void SetGain(GamePhase phase, double value)
	{
		lock (gate)
		{
			playback = PlaybackTimeline.SetGain(playback, phase, value);
			Broadcast();
		}
	}

	public void SwitchPhase()
	{
		lock (gate)
		{
			var target = playback.ActivePhase == GamePhase.Day ? GamePhase.Night : GamePhase.Day;
			var activeCurrent = ToTrack(playlists.At(playback.ActivePhase, playback.ActiveCursor));
			var targetCurrent = ToTrack(playlists.At(target, playback.Cursor(target)));
			playback = PlaybackTimeline.SwitchPhase(playback, activeCurrent, targetCurrent, Now());
			Broadcast();
		}
	}

	public bool Add(GamePhase phase, string songId)
	{
		lock (gate)
		{
			if (!pool.EntriesById.TryGetValue(songId, out var entry))
				return false;

			var added = Playlists.Add(playlists.Entries(phase), ToEntry(entry));
			playlists = playlists.Replace(phase, added);
			playback = PlaybackTimeline.ReindexAfterAdd(playback, phase);
			Save(phase, added);
			Broadcast();
			return true;
		}
	}

	public bool Remove(GamePhase phase, int index)
	{
		lock (gate)
		{
			if (!InRange(phase, index))
				return false;

			var entries = playlists.Entries(phase);
			var removed = Playlists.RemoveAt(entries, index);
			playlists = playlists.Replace(phase, removed);
			playback = PlaybackTimeline.ReindexAfterRemove(playback, phase, index, entries.Length,
					ToTracks(removed), Now());
			Save(phase, removed);
			Broadcast();
			return true;
		}
	}

	public void Shuffle(GamePhase phase)
	{
		lock (gate)
		{
			var pinned = PlaybackTimeline.CursorOfStartedSong(playback, phase);
			var shuffled = Playlists.Shuffle(playlists.Entries(phase), pinned, Random.Shared);
			playlists = playlists.Replace(phase, shuffled);
			playback = PlaybackTimeline.ReindexAfterShuffle(playback, phase);
			Save(phase, shuffled);
			Broadcast();
		}
	}

	public bool Move(GamePhase phase, int oldIndex, int newIndex)
	{
		lock (gate)
		{
			if (!InRange(phase, oldIndex))
				return false;

			var entries = playlists.Entries(phase);
			var moved = Playlists.Move(entries, oldIndex, newIndex);
			playlists = playlists.Replace(phase, moved);
			playback =
					PlaybackTimeline.ReindexAfterMove(playback, phase, oldIndex, newIndex, entries.Length);
			Save(phase, moved);
			Broadcast();
			return true;
		}
	}

	public void Tick()
	{
		lock (gate)
		{
			if (!playback.IsPlaying)
				return;

			var now = Now();
			var previous = playback;
			playback = PlaybackTimeline.Tick(playback, ToTracks(playlists.Entries(playback.ActivePhase)),
					now);
			if (PlaybackTimeline.PlaybackJumped(previous, playback, now))
				Broadcast();
		}
	}

	public Channel<PlaybackEvent> Subscribe()
	{
		lock (gate)
		{
			var channel = broadcaster.Add();
			channel.Writer.TryWrite(CurrentSnapshot());
			return channel;
		}
	}

	public void Unsubscribe(Channel<PlaybackEvent> channel)
	{
		lock (gate)
		{
			broadcaster.Remove(channel);
		}
	}

	private bool InRange(GamePhase phase, int index)
		=> index >= 0 && index < playlists.Entries(phase).Length;

	private static Channel<PlaybackEvent> CreateBoundedChannel()
		=> Channel.CreateBounded<PlaybackEvent>(
			new BoundedChannelOptions(SnapshotBacklog) { FullMode = BoundedChannelFullMode.DropOldest });

	private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	private static PhasePlayback InitialPhase(int count)
		=> new(Cursor: count > 0 ? 0 : null);

	private static PlaylistEntry[] ToEntries(IReadOnlyList<PoolEntry> entries)
		=> entries.Select(ToEntry).ToArray();

	private static PlaylistEntry ToEntry(PoolEntry entry)
		=> new(entry.Id, entry.Song.Path, entry.Song.Title, entry.Song.Artist,
					entry.Song.Length.TotalSeconds);

	private static Track[] ToTracks(PlaylistEntry[] entries)
		=> entries.Select(entry => ToTrack(entry)!).ToArray();

	private static Track? ToTrack(PlaylistEntry? entry)
		=> entry is null ? null : new Track(entry.Id, entry.Length);

	private void Save(GamePhase phase, PlaylistEntry[] entries)
		=> playlistStore.Save(phase, entries.Select(entry => entry.Path).ToArray());

	private void Broadcast()
		=> broadcaster.Publish(CurrentSnapshot());

	private SnapshotEvent CurrentSnapshot() => new(renderFrame(playback, playlists));
}
