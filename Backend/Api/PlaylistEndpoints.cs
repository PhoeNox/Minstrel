// ReSharper disable ClassNeverInstantiated.Global
namespace Backend.Api;

using Core;
using Sessions.Playback;

public sealed record AddCommand(GamePhase Phase, string SongId);
public sealed record RemoveCommand(GamePhase Phase, int Index);
public sealed record ShuffleCommand(GamePhase Phase);
public sealed record MoveCommand(GamePhase Phase, int OldIndex, int NewIndex);

public static class PlaylistEndpoints
{
	public static void MapPlaylistEndpoints(this WebApplication app)
	{
		app.MapPost("/playlist/add", Add);
		app.MapPost("/playlist/remove", Remove);
		app.MapPost("/playlist/shuffle", Shuffle);
		app.MapPost("/playlist/move", Move);
	}

	private static IResult Add(AddCommand? command, PlaybackSession session)
	{
		if (command is null)
			return Results.BadRequest("A phase and song id are required.");

		session.Add(command.Phase, command.SongId);
		return Results.NoContent();
	}

	private static IResult Remove(RemoveCommand? command, PlaybackSession session)
	{
		if (command is null)
			return Results.BadRequest("A phase and index are required.");

		session.Remove(command.Phase, command.Index);
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

		session.Move(command.Phase, command.OldIndex, command.NewIndex);
		return Results.NoContent();
	}
}
