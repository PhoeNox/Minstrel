namespace FileSystem;

using Core;

public class LibraryProvider(IFileSystemProvider fileSystem)
{
	public Song[] LoadLibrary()
	{
		var songFilePaths = fileSystem.EnumerateFiles();
		return fileSystem.ReadSongs(songFilePaths);
	}
}
