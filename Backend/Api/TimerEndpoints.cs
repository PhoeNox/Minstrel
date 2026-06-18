// ReSharper disable ClassNeverInstantiated.Global
namespace Backend.Api;

using System.Text.Json;
using Backend.Sessions.Timer;
using Core.Timer;
using Microsoft.Extensions.Options;

public sealed record TimerStartCommand(double Duration);

public sealed record TimerDto(bool Running, long AnchorTimestamp, double DurationLeftAtAnchor);

public static class TimerEndpoints
{
	public static void MapTimerEndpoints(this WebApplication app)
	{
		app.MapGet("/timer/sse", StreamTimer);
		app.MapPost("/timer/start", StartTimer);
		app.MapPost("/timer/stop", StopTimer);
	}

	private static async Task StreamTimer(HttpContext context, TimerSession session, IOptions<GongOptions> gong, CancellationToken cancellation)
	{
		context.Response.Headers.ContentType = "text/event-stream";
		context.Response.Headers.CacheControl = "no-cache";

		var channel = session.Subscribe();
		try
		{
			await foreach (var message in channel.Reader.ReadAllAsync(cancellation))
			{
				await WriteEvent(context, message, gong.Value.Gain, cancellation);
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

	private static Task WriteEvent(HttpContext context, TimerEvent message, double gongGain, CancellationToken cancellation) =>
		message switch
		{
			GongEvent => context.Response.WriteAsync(
				$"event: gong\ndata: {JsonSerializer.Serialize(new { gain = gongGain }, SseJson.Options)}\n\n",
				cancellation),
			TimerSnapshotEvent snapshot => context.Response.WriteAsync(
				$"data: {JsonSerializer.Serialize(ToDto(snapshot.Timer), SseJson.Options)}\n\n",
				cancellation),
			_ => Task.CompletedTask,
		};

	private static TimerDto? ToDto(TimerAnchor timer) =>
		timer.Running ? new TimerDto(timer.Running, timer.AnchorTimestamp, timer.DurationLeftAtAnchor) : null;

	private static IResult StartTimer(TimerStartCommand? command, TimerSession session)
	{
		if (command is null || command.Duration <= 0)
			return Results.BadRequest("A positive duration is required.");

		session.Start(TimeSpan.FromSeconds(command.Duration));
		return Results.NoContent();
	}

	private static IResult StopTimer(TimerSession session)
	{
		session.Stop();
		return Results.NoContent();
	}
}
