namespace Backend.Features.Library;

using Session;

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
	
	private static IResult GetLibrary(SongPool pool)
	{
		var songsInLibrary = pool.All
				.Select(entry => pool.ToSongDto(entry.Id))
				.ToArray();
		return Results.Ok(songsInLibrary);
	}
	
	private static IResult Add(AddCommand? command, LiveSession session)
	{
		if (command is null)
			return Results.BadRequest("A phase and song id are required.");

		session.Add(command.Phase, command.SongId);
		return Results.NoContent();
	}

	private static IResult Remove(RemoveCommand? command, LiveSession session)
	{
		if (command is null)
			return Results.BadRequest("A phase and index are required.");

		session.Remove(command.Phase, command.Index);
		return Results.NoContent();
	}

	private static IResult Shuffle(ShuffleCommand? command, LiveSession session)
	{
		if (command is null)
			return Results.BadRequest("A phase is required.");

		session.Shuffle(command.Phase);
		return Results.NoContent();
	}

	private static IResult Move(MoveCommand? command, LiveSession session)
	{
		if (command is null)
			return Results.BadRequest("A phase and indices are required.");

		session.Move(command.Phase, command.OldIndex, command.NewIndex);
		return Results.NoContent();
	}
}
