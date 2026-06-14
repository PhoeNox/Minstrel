namespace Backend.Contracts;

using Backend.Library;
using Backend.Playback;
using Core;

public sealed record SongDto(string Id, string Title, string Artist, double Length);

public sealed record PlaylistDto(SongDto[] Songs, string? CurrentSongId);

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
	public static StateSnapshot ToSnapshot(TimelineState state, SongLibrary library) =>
		new(
			Phase: state.ActivePhase,
			IsPlaying: state.IsPlaying,
			CurrentSongId: state.CurrentSongId,
			Playlists: new PlaylistsDto(ToPlaylistDto(state.Day, library), ToPlaylistDto(state.Night, library)),
			Position: ToPositionDto(state.Position));

	private static PlaylistDto ToPlaylistDto(TimelinePlaylist playlist, SongLibrary library) =>
		new(playlist.Songs.Select(song => library.ToSongDto(song.Id)).ToArray(), playlist.CurrentSongId);

	private static PositionDto ToPositionDto(PositionAnchor anchor) =>
		new(anchor.SongId, anchor.Offset, anchor.AnchorTimestamp, anchor.IsPlaying);
}
