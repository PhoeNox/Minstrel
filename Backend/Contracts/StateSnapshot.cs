namespace Backend.Contracts;

using Backend.Library;
using Backend.Playback;

public sealed record SongDto(string Id, string Title, string Artist, double Length);

public sealed record StateSnapshot(bool IsPlaying, string? CurrentSongId, SongDto[] Songs);

public static class SnapshotMapper
{
	public static StateSnapshot ToSnapshot(TimelineState state, IReadOnlyList<LibraryEntry> entries) =>
		new(
			IsPlaying: state.IsPlaying,
			CurrentSongId: state.CurrentSongId,
			Songs: entries.Select(ToSongDto).ToArray());

	private static SongDto ToSongDto(LibraryEntry entry) =>
		new(entry.Id, entry.Song.Title, entry.Song.Artist, entry.Song.Length.TotalSeconds);
}
