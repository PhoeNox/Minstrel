namespace Backend.Api;

using Backend.Features.Library;

public static class LibraryEndpoints
{
	public static void MapLibraryEndpoints(this WebApplication app)
	{
		app.MapGet("/library", GetLibrary);
	}

	private static IResult GetLibrary(SongPool pool)
	{
		var songsInLibrary = pool.All
				.Select(entry => pool.ToSongDto(entry.Id))
				.ToArray();
		return Results.Ok(songsInLibrary);
	}
}
