namespace Backend.Contracts;

using Core;
using Features.Playback;

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
	public static StateSnapshot ToSnapshot(TimelineState state) =>
		new(
			Phase: state.ActivePhase,
			IsPlaying: state.IsPlaying,
			CurrentSongId: state.CurrentSongId,
			Playlists: new PlaylistsDto(ToPlaylistDto(state.Day), ToPlaylistDto(state.Night)),
			Position: ToPositionDto(state.Position));

	private static PlaylistDto ToPlaylistDto(TimelinePlaylist playlist) =>
		new(
			playlist.Songs.Select(ToSongDto).ToArray(),
			playlist.CurrentIndex,
			playlist.Gain,
			playlist.ResumeOffset);

	private static SongDto ToSongDto(TimelineSong song) =>
		new(song.Id, song.Title, song.Artist, song.Length);

	private static PositionDto ToPositionDto(PositionAnchor anchor) =>
		new(anchor.SongId, anchor.Offset, anchor.AnchorTimestamp, anchor.IsPlaying);
}
