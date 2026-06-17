namespace Backend.Features.Playlist;

public sealed record PlaylistEntry(string Id, string Path, string Title, string Artist, double Length);
