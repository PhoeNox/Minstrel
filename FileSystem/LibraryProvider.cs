namespace FileSystem;

using Core;

public class LibraryProvider(IFileSystemProvider fileSystem)
{
	public Song[] LoadLibrary()
	{
		var songsPaths = fileSystem.EnumerateSongs();
		return fileSystem.ReadSongs(songsPaths);
	}
}
