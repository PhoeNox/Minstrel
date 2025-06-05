namespace Core;

public record Song(
	string Path,
	string Title,
	string Artist,
	string Album,
	TimeSpan Length);