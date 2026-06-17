namespace Backend.Sessions;

using Core.Playback;
using Core.Playlist;

public abstract record SessionEvent;

public sealed record SnapshotEvent(PlaybackState Playback, PlaylistBook Playlists) : SessionEvent;
