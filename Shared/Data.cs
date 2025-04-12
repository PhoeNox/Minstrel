namespace Shared;

public record Playlist(Song[] Songs);

public record Song(
	string Title,
	string Artist,
	string Album,
	TimeSpan Length);

public enum GamePhase
{
	Day,
	Night,
}