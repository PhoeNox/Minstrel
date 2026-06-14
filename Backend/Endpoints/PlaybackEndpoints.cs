namespace Backend.Endpoints;

using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Commands;
using Backend.Library;
using Backend.Playback;
using Microsoft.AspNetCore.StaticFiles;

public static class PlaybackEndpoints
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
	{
		Converters = { new JsonStringEnumConverter() },
	};

	private static readonly FileExtensionContentTypeProvider ContentTypes = new();

	public static void MapPlaybackEndpoints(this WebApplication app)
	{
		app.MapGet("/sse", StreamState);
		app.MapGet("/audio/{songId}", StreamAudio);
		app.MapPost("/commands/play", Play);
		app.MapPost("/commands/pause", Pause);
		app.MapPost("/commands/select", Select);
		app.MapPost("/commands/move", Move);
	}

	private static async Task StreamState(HttpContext context, PlaybackSession session, CancellationToken cancellation)
	{
		context.Response.Headers.ContentType = "text/event-stream";
		context.Response.Headers.CacheControl = "no-cache";

		var channel = session.Subscribe();
		try
		{
			await foreach (var snapshot in channel.Reader.ReadAllAsync(cancellation))
			{
				var json = JsonSerializer.Serialize(snapshot, JsonOptions);
				await context.Response.WriteAsync($"data: {json}\n\n", cancellation);
				await context.Response.Body.FlushAsync(cancellation);
			}
		}
		catch (OperationCanceledException)
		{
		}
		finally
		{
			session.Unsubscribe(channel);
		}
	}

	private static IResult StreamAudio(string songId, SongLibrary library)
	{
		if (!library.TryGetPath(songId, out var path))
		{
			return Results.NotFound();
		}

		var contentType = ContentTypes.TryGetContentType(path, out var resolved)
			? resolved
			: "application/octet-stream";
		return Results.File(Path.GetFullPath(path), contentType, enableRangeProcessing: true);
	}

	private static IResult Play(PlayCommand? command, PlaybackSession session)
	{
		if (command?.SongId is { } songId)
		{
			session.Play(songId);
			return Results.NoContent();
		}

		if (session.HasCurrentSong)
		{
			session.Resume();
			return Results.NoContent();
		}

		return session.PlayCurrent()
			? Results.NoContent()
			: Results.BadRequest("The active playlist is empty.");
	}

	private static IResult Pause(PlaybackSession session)
	{
		session.Pause();
		return Results.NoContent();
	}

	private static IResult Select(SelectCommand? command, PlaybackSession session)
	{
		if (command is null)
			return Results.BadRequest("A phase and song id are required.");

		session.SelectSong(command.Phase, command.SongId);
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
