namespace Core;

public record Playlists(
	Playlist DayPlaylist,
	Playlist NightPlaylist);

public record Playlist(Song[] Songs);

public record Song(
	string Path,
	string Title,
	string Artist,
	string Album,
	TimeSpan Length);

public enum GamePhase
{
	Day,
	Night,
}