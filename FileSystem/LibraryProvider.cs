namespace FileSystem;

using Core;

public class LibraryProvider(IFileSystemProvider fileSystem)
{
	public Song[] LoadLibrary()
		=> fileSystem.ReadSongs(fileSystem.EnumerateFiles());
}
