namespace Backend.Tests.Playback;

using Features.Playback;

internal static class TimelineFixtures
{
	public static TimelineSong Song(string id, double length) =>
		new(id, Path: $"{id}.mp3", Title: id, Artist: id, length);
}
