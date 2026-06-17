namespace FileSystem;

using Core;

public class PlaylistProvider(IFileSystemProvider fileSystem)
{
	public (Song[] Day, Song[] Night) LoadPlaylists()
	{
		var daySongs = Load(GamePhase.Day);
		var nightSongs = Load(GamePhase.Night);
		return (daySongs, nightSongs);
	}
	
	private Song[] Load(GamePhase phase)
	{
		var playlistPath = GetPlaylistPath(phase);
		var songPaths = fileSystem.ReadLines(playlistPath);
		return fileSystem.ReadSongs(songPaths);
	}

	public void Save(GamePhase phase, IReadOnlyList<string> songPaths)
	{
		var playlistPath = GetPlaylistPath(phase);
		var relativeSongPaths = songPaths
				.Select(path => Path.GetRelativePath(fileSystem.MusicDirectory, path));
		fileSystem.WriteLines(playlistPath, relativeSongPaths);
	}

	private string GetPlaylistPath(GamePhase phase)
	{
		var fileName = phase == GamePhase.Day ? "day.m3u" : "night.m3u";
		return Path.Combine(fileSystem.MusicDirectory, fileName);
	}
}
