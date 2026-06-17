// ReSharper disable ClassNeverInstantiated.Global
namespace Backend.Api;

using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Features.Timer;
using Microsoft.Extensions.Options;

public sealed record TimerStartCommand(double Duration);

public static class TimerEndpoints
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
	{
		Converters = { new JsonStringEnumConverter() },
	};

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
				$"event: gong\ndata: {JsonSerializer.Serialize(new { gain = gongGain }, JsonOptions)}\n\n",
				cancellation),
			TimerSnapshotEvent snapshot => context.Response.WriteAsync(
				$"data: {JsonSerializer.Serialize(snapshot.Timer, JsonOptions)}\n\n",
				cancellation),
			_ => Task.CompletedTask,
		};

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
