namespace Core.Tests.Playback;

using Backend.Features.Playback;
using Core;
using Playlist;

internal static class Fixtures
{
	public static PlaylistEntry Entry(string id, double length) =>
		new(id, Path: $"{id}.mp3", Title: id, Artist: id, length);

	public static Track Track(string id, double length) => new(id, length);

	public static Track[] Tracks(params (string Id, double Length)[] songs) =>
		songs.Select(song => new Track(song.Id, song.Length)).ToArray();

	public static PlaylistBook Book(PlaylistEntry[] day, PlaylistEntry[] night) => new(day, night);

	public static Track? TrackAt(PlaylistBook book, GamePhase phase, int? index) =>
		book.At(phase, index) is { } entry ? new Track(entry.Id, entry.Length) : null;

	public static PlaybackState Switch(PlaylistBook book, PlaybackState state, long now)
	{
		var target = state.ActivePhase == GamePhase.Day ? GamePhase.Night : GamePhase.Day;
		var activeCurrent = TrackAt(book, state.ActivePhase, state.ActiveCursor);
		var targetCurrent = TrackAt(book, target, state.Cursor(target));
		return PlaybackTimeline.SwitchPhase(state, activeCurrent, targetCurrent, now);
	}

	public static PlaybackState SelectIn(PlaylistBook book, PlaybackState state, GamePhase phase, int index, long now) =>
		PlaybackTimeline.Select(state, phase, index, TrackAt(book, phase, index), now);
}
