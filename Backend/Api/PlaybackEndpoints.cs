// ReSharper disable ClassNeverInstantiated.Global
namespace Backend.Api;

using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Sessions;
using Core;
using Core.Library;
using Microsoft.AspNetCore.StaticFiles;

public sealed record PlayCommand(string? SongId);
public sealed record SelectCommand(GamePhase Phase, int Index);
public sealed record SetGainCommand(GamePhase Phase, double Value);

public static class PlaybackEndpoints
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
	{
		Converters = { new JsonStringEnumConverter() },
	};

	private static readonly FileExtensionContentTypeProvider ContentTypes = new();

	public static void MapPlaybackEndpoints(this WebApplication app)
	{
		app.MapGet("/playback/sse", StreamState);
		app.MapGet("/playback/audio/{songId}", StreamAudio);

		app.MapPost("/playback/play", Play);
		app.MapPost("/playback/pause", Pause);
		app.MapPost("/playback/switch-phase", SwitchPhase);
		app.MapPost("/playback/select", Select);

		app.MapPost("/playback/set-gain", SetGain);
	}

	private static async Task StreamState(HttpContext context, LiveSession session, CancellationToken cancellation)
	{
		context.Response.Headers.ContentType = "text/event-stream";
		context.Response.Headers.CacheControl = "no-cache";

		var channel = session.Subscribe();
		try
		{
			await foreach (var message in channel.Reader.ReadAllAsync(cancellation))
			{
				await WriteEvent(context, message, cancellation);
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

	private static Task WriteEvent(HttpContext context, SessionEvent message, CancellationToken cancellation) =>
		message switch
		{
			SnapshotEvent snapshot => context.Response.WriteAsync(
				$"data: {JsonSerializer.Serialize(SnapshotMapper.ToSnapshot(snapshot.Playback, snapshot.Playlists), JsonOptions)}\n\n",
				cancellation),
			_ => Task.CompletedTask,
		};

	private static IResult StreamAudio(string songId, SongPool pool)
	{
		if (!pool.EntriesById.TryGetValue(songId, out var entry))
			return Results.NotFound();

		var path = entry.Song.Path;
		var contentType = ContentTypes.TryGetContentType(path, out var resolved)
			? resolved
			: "application/octet-stream";
		return Results.File(Path.GetFullPath(path), contentType, enableRangeProcessing: true);
	}

	private static IResult Play(PlayCommand? command, LiveSession session)
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

	private static IResult Pause(LiveSession session)
	{
		session.Pause();
		return Results.NoContent();
	}

	private static IResult SwitchPhase(LiveSession session)
	{
		session.SwitchPhase();
		return Results.NoContent();
	}

	private static IResult Select(SelectCommand? command, LiveSession session)
	{
		if (command is null)
			return Results.BadRequest("A phase and index are required.");

		session.Select(command.Phase, command.Index);
		return Results.NoContent();
	}

	private static IResult SetGain(SetGainCommand? command, LiveSession session)
	{
		if (command is null)
			return Results.BadRequest("A phase and gain value are required.");

		session.SetGain(command.Phase, command.Value);
		return Results.NoContent();
	}

}
