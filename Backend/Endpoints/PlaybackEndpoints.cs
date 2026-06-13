namespace Backend.Endpoints;

using System.Text.Json;
using Backend.Commands;
using Backend.Library;
using Backend.Playback;
using Microsoft.AspNetCore.StaticFiles;

public static class PlaybackEndpoints
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	private static readonly FileExtensionContentTypeProvider ContentTypes = new();

	public static void MapPlaybackEndpoints(this WebApplication app)
	{
		app.MapGet("/sse", StreamState);
		app.MapGet("/audio/{songId}", StreamAudio);
		app.MapPost("/commands/play", Play);
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

	private static IResult Play(PlayCommand? command, PlaybackSession session, SongLibrary library)
	{
		var songId = command?.SongId ?? library.Entries.FirstOrDefault()?.Id;
		if (songId is null)
		{
			return Results.BadRequest("The song library is empty.");
		}

		session.Play(songId);
		return Results.NoContent();
	}
}
