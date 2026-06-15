namespace Backend.Contracts;

using Backend.Library;
using Backend.Playback;
using Backend.Timer;
using Core;

public sealed record SongDto(string Id, string Title, string Artist, double Length);

public sealed record PlaylistDto(SongDto[] Songs, int? CurrentIndex, double Gain, double ResumeOffset);

public sealed record PlaylistsDto(PlaylistDto Day, PlaylistDto Night);

public sealed record PositionDto(string? SongId, double Offset, long AnchorTimestamp, bool IsPlaying);

public sealed record TimerDto(bool Running, long AnchorTimestamp, double DurationLeftAtAnchor);

public sealed record StateSnapshot(
	GamePhase Phase,
	bool IsPlaying,
	string? CurrentSongId,
	PlaylistsDto Playlists,
	PositionDto Position,
	TimerDto? Timer);

public static class SnapshotMapper
{
	public static StateSnapshot ToSnapshot(TimelineState state, TimerAnchor timer, SongLibrary library) =>
		new(
			Phase: state.ActivePhase,
			IsPlaying: state.IsPlaying,
			CurrentSongId: state.CurrentSongId,
			Playlists: new PlaylistsDto(ToPlaylistDto(state.Day, library), ToPlaylistDto(state.Night, library)),
			Position: ToPositionDto(state.Position),
			Timer: ToTimerDto(timer));

	private static TimerDto? ToTimerDto(TimerAnchor timer) =>
		timer.Running ? new TimerDto(timer.Running, timer.AnchorTimestamp, timer.DurationLeftAtAnchor) : null;

	private static PlaylistDto ToPlaylistDto(TimelinePlaylist playlist, SongLibrary library) =>
		new(
			playlist.Songs.Select(song => library.ToSongDto(song.Id)).ToArray(),
			playlist.CurrentIndex,
			playlist.Gain,
			playlist.ResumeOffset);

	private static PositionDto ToPositionDto(PositionAnchor anchor) =>
		new(anchor.SongId, anchor.Offset, anchor.AnchorTimestamp, anchor.IsPlaying);
}
