namespace App;

public static class TimeSpanExtensions
{
	public static string ToSongLength(this TimeSpan timeSpan)
		=> $"{(int) timeSpan.TotalMinutes}:{timeSpan.Seconds:D2}";
}
