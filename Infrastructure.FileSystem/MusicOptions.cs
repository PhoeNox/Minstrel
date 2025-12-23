namespace Infrastructure.FileSystem;

public record MusicOptions
{
	public const string Section = "Music";
	public string Directory { get; init; } = string.Empty;
}
