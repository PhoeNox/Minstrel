namespace Backend.Contracts;

using Core;
using Core.Playlist;
using Core.Playback;

public sealed record SongDto(string Id, string Title, string Artist, double Length);

public sealed record PlaylistDto(SongDto[] Songs, int? CurrentIndex, double Gain, double ResumeOffset);

public sealed record PlaylistsDto(PlaylistDto Day, PlaylistDto Night);

public sealed record PositionDto(string? SongId, double Offset, long AnchorTimestamp, bool IsPlaying);

public sealed record StateSnapshot(
	GamePhase Phase,
	bool IsPlaying,
	string? CurrentSongId,
	PlaylistsDto Playlists,
	PositionDto Position);

public static class SnapshotMapper
{
	public static StateSnapshot ToSnapshot(PlaybackState playback, PlaylistBook playlists) =>
		new(
			Phase: playback.ActivePhase,
			IsPlaying: playback.IsPlaying,
			CurrentSongId: playback.CurrentSongId,
			Playlists: new PlaylistsDto(
				ToPlaylistDto(playlists.Day, playback.Day),
				ToPlaylistDto(playlists.Night, playback.Night)),
			Position: ToPositionDto(playback.Position));

	private static PlaylistDto ToPlaylistDto(PlaylistEntry[] entries, PhasePlayback phase) =>
		new(
			entries.Select(ToSongDto).ToArray(),
			phase.Cursor,
			phase.Gain,
			phase.ResumeOffset);

	private static SongDto ToSongDto(PlaylistEntry entry) =>
		new(entry.Id, entry.Title, entry.Artist, entry.Length);

	private static PositionDto ToPositionDto(PositionAnchor anchor) =>
		new(anchor.SongId, anchor.Offset, anchor.AnchorTimestamp, anchor.IsPlaying);
}
