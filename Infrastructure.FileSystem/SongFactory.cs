namespace Infrastructure.FileSystem;

using Core;

public static class SongFactory
{
	public static Song Create(string path)
	{
		using var tags = TagLib.File.Create(path);
		return new Song
		(
				Path: path,
				Title: tags.Tag.Title ?? Path.GetFileNameWithoutExtension(path),
				Artist: string.Join(", ", tags.Tag.Performers),
				Album: tags.Tag.Album ?? "Unknown Album",
				Length: tags.Properties.Duration
		);
	}
}
