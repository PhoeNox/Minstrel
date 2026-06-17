namespace Backend.Sessions.Playback;

using Core.Playback;
using Core.Playlist;

public abstract record PlaybackEvent;

public sealed record SnapshotEvent(PlaybackState Playback, PlaylistBook Playlists) : PlaybackEvent;
