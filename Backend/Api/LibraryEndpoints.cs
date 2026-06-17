namespace Backend.Api;

using Contracts;
using Core.Library;

public static class LibraryEndpoints
{
	public static void MapLibraryEndpoints(this WebApplication app)
	{
		app.MapGet("/library", GetLibrary);
	}

	private static IResult GetLibrary(SongPool pool)
	{
		var songsInLibrary = pool.All
				.Select(ToSongDto)
				.ToArray();
		return Results.Ok(songsInLibrary);
	}

	private static SongDto ToSongDto(PoolEntry entry)
	{
		var song = entry.Song;
		return new SongDto(entry.Id, song.Title, song.Artist, song.Length.TotalSeconds);
	}
}
