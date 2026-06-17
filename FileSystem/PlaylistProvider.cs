namespace FileSystem;

using Core;

public class PlaylistProvider(IFileSystemProvider fileSystem)
{
	public (Song[] Day, Song[] Night) LoadPlaylists()
		=> (Load(GamePhase.Day), Load(GamePhase.Night));

	public void Save(GamePhase phase, IReadOnlyList<string> songPaths)
		=> fileSystem.WriteLines(PlaylistPath(phase),
				songPaths.Select(path => Path.GetRelativePath(fileSystem.MusicDirectory, path)));

	private Song[] Load(GamePhase phase)
		=> fileSystem.ReadSongs(fileSystem.ReadLines(PlaylistPath(phase)));

	private string PlaylistPath(GamePhase phase)
	{
		var fileName = phase == GamePhase.Day ? "day.m3u" : "night.m3u";
		return Path.Combine(fileSystem.MusicDirectory, fileName);
	}
}
