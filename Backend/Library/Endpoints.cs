namespace Backend.Library;

using Playback;

public static class Endpoints
{
	public static void MapLibraryEndpoints(this WebApplication app)
	{
		app.MapGet("/library", GetLibrary);
		
		app.MapPost("/commands/add", Add);
		app.MapPost("/commands/remove", Remove);
		app.MapPost("/commands/shuffle", Shuffle);
		app.MapPost("/commands/move", Move);
	}
	
	private static IResult GetLibrary(SongLibrary library)
	{
		var songsInLibrary = library.All
				.Select(entry => library.ToSongDto(entry.Id))
				.ToArray();
		return Results.Ok(songsInLibrary);
	}
	
	private static IResult Add(AddCommand? command, PlaybackSession session)
	{
		if (command is null)
			return Results.BadRequest("A phase and song id are required.");

		session.AddSong(command.Phase, command.SongId);
		return Results.NoContent();
	}

	private static IResult Remove(RemoveCommand? command, PlaybackSession session)
	{
		if (command is null)
			return Results.BadRequest("A phase and index are required.");

		session.RemoveSong(command.Phase, command.Index);
		return Results.NoContent();
	}

	private static IResult Shuffle(ShuffleCommand? command, PlaybackSession session)
	{
		if (command is null)
			return Results.BadRequest("A phase is required.");

		session.Shuffle(command.Phase);
		return Results.NoContent();
	}

	private static IResult Move(MoveCommand? command, PlaybackSession session)
	{
		if (command is null)
			return Results.BadRequest("A phase and indices are required.");

		session.MoveSong(command.Phase, command.OldIndex, command.NewIndex);
		return Results.NoContent();
	}
}
