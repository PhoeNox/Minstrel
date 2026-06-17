namespace Backend.Features.Timer;

using Playback;

public static class Endpoints
{
	public static void MapTimerEndpoints(this WebApplication app)
	{
		app.MapPost("/commands/timer-start", StartTimer);
		app.MapPost("/commands/timer-stop", StopTimer);
	}

	private static IResult StartTimer(TimerStartCommand? command, PlaybackSession session)
	{
		if (command is null || command.DurationInSecs <= 0)
			return Results.BadRequest("A positive duration is required.");

		var duration = TimeSpan.FromSeconds(command.DurationInSecs);
		session.StartTimer(duration);
		return Results.NoContent();
	}

	private static IResult StopTimer(PlaybackSession session)
	{
		session.StopTimer();
		return Results.NoContent();
	}
}
