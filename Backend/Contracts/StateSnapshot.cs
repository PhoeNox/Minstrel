namespace Backend.Contracts;

using Backend.Library;
using Backend.Playback;

public sealed record SongDto(string Id, string Title, string Artist, double Length);

public sealed record PositionDto(string? SongId, double Offset, long AnchorTimestamp, bool IsPlaying);

public sealed record StateSnapshot(bool IsPlaying, string? CurrentSongId, SongDto[] Songs, PositionDto Position);

public static class SnapshotMapper
{
	public static StateSnapshot ToSnapshot(TimelineState state, IReadOnlyList<LibraryEntry> entries) =>
		new(
			IsPlaying: state.IsPlaying,
			CurrentSongId: state.CurrentSongId,
			Songs: entries.Select(ToSongDto).ToArray(),
			Position: ToPositionDto(state.Position));

	private static SongDto ToSongDto(LibraryEntry entry) =>
		new(entry.Id, entry.Song.Title, entry.Song.Artist, entry.Song.Length.TotalSeconds);

	private static PositionDto ToPositionDto(PositionAnchor anchor) =>
		new(anchor.SongId, anchor.Offset, anchor.AnchorTimestamp, anchor.IsPlaying);
}
