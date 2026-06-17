namespace Backend.Features.Playback;

using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Contracts;
using Infrastructure.Network;
using Library;
using Microsoft.AspNetCore.StaticFiles;

public static class Endpoints
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
	{
		Converters = { new JsonStringEnumConverter() },
	};

	private static readonly FileExtensionContentTypeProvider ContentTypes = new();

	public static void MapPlaybackEndpoints(this WebApplication app)
	{
		app.MapGet("/sse", StreamState);
		app.MapGet("/connection", GetConnection);
		app.MapGet("/version", GetVersion);
		app.MapGet("/audio/{songId}", StreamAudio);
		
		app.MapPost("/commands/play", Play);
		app.MapPost("/commands/pause", Pause);
		app.MapPost("/commands/switch-phase", SwitchPhase);
		app.MapPost("/commands/select", Select);
		
		app.MapPost("/commands/set-gain", SetGain);
	}

	private static async Task StreamState(HttpContext context, PlaybackSession session, CancellationToken cancellation)
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
				$"data: {JsonSerializer.Serialize(snapshot.Snapshot, JsonOptions)}\n\n",
				cancellation),
			_ => Task.CompletedTask,
		};

	private static IResult GetConnection(INetworkProvider network, IConfiguration configuration)
	{
		var host = ResolveHost(configuration["Host"], network);
		var info = ConnectionInfo.For(host, configuration["Port"] ?? "5757");
		return Results.Ok(info);
	}

	private static IResult GetVersion() => Results.Ok(VersionInfo.Current);

	private static string ResolveHost(string? configuredHost, INetworkProvider network) =>
		string.IsNullOrWhiteSpace(configuredHost)
			? network.GetLocalIpAddress()
			: configuredHost.Trim();

	private static IResult StreamAudio(string songId, SongPool pool)
	{
		if (!pool.TryGetPath(songId, out var path))
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

	private static IResult SwitchPhase(PlaybackSession session)
	{
		session.SwitchPhase();
		return Results.NoContent();
	}

	private static IResult Select(SelectCommand? command, PlaybackSession session)
	{
		if (command is null)
			return Results.BadRequest("A phase and index are required.");

		session.SelectSong(command.Phase, command.Index);
		return Results.NoContent();
	}

	private static IResult SetGain(SetGainCommand? command, PlaybackSession session)
	{
		if (command is null)
			return Results.BadRequest("A phase and gain value are required.");

		session.SetGain(command.Phase, command.Value);
		return Results.NoContent();
	}

}
