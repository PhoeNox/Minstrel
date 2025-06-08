namespace Core;

public record Playlist(Song[] Songs)
{
	public Song? CurrentSong { get; init; } = Songs.FirstOrDefault();
	public float Gain { get; init; } = 1;
}
