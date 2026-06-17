namespace Backend.Features.Playlist;

using Core;

public sealed record PlaylistBook(PlaylistEntry[] Day, PlaylistEntry[] Night)
{
	public static PlaylistBook Empty { get; } = new([], []);

	public PlaylistEntry[] Entries(GamePhase phase) => phase == GamePhase.Day ? Day : Night;

	public PlaylistEntry? At(GamePhase phase, int? index)
	{
		var entries = Entries(phase);
		return index is { } i && i >= 0 && i < entries.Length ? entries[i] : null;
	}

	public PlaylistBook Replace(GamePhase phase, PlaylistEntry[] entries) =>
		phase == GamePhase.Day ? this with { Day = entries } : this with { Night = entries };
}
