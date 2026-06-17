namespace Core;

public record Song(
	string Path,
	string Title,
	string Artist,
	TimeSpan Length);
