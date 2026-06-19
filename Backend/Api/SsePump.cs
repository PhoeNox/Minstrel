namespace Backend.Api;

using System.Threading.Channels;

internal static class SsePump
{
	// Streams broadcast only on state change, so an idle connection can be dropped silently by an
	// intermediary without either side noticing until the next write. A periodic comment keeps the
	// connection warm and surfaces a dead one on the next heartbeat write.
	private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);

	public static async Task Run<TEvent>(
		HttpContext context,
		Channel<TEvent> channel,
		Func<HttpContext, TEvent, CancellationToken, Task> writeEvent,
		Action unsubscribe,
		CancellationToken cancellation)
	{
		context.Response.Headers.ContentType = "text/event-stream";
		context.Response.Headers.CacheControl = "no-cache";

		try
		{
			while (await WaitForEventOrHeartbeat(context, channel.Reader, cancellation))
			{
				while (channel.Reader.TryRead(out var message))
					await writeEvent(context, message, cancellation);
				await context.Response.Body.FlushAsync(cancellation);
			}
		}
		catch (OperationCanceledException)
		{
			// Client closed the stream or the request was cancelled — a clean end.
		}
		catch (IOException)
		{
			// Broken pipe / connection reset: the client aborted a live stream. Normal disconnect.
		}
		finally
		{
			unsubscribe();
		}
	}

	// Waits for the next event, returning false once the channel completes. When no event arrives
	// within the heartbeat interval, emits a comment line to keep the connection warm and returns
	// true to resume waiting; a genuine request cancellation propagates instead.
	private static async Task<bool> WaitForEventOrHeartbeat<TEvent>(
		HttpContext context,
		ChannelReader<TEvent> reader,
		CancellationToken cancellation)
	{
		using var heartbeat = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
		heartbeat.CancelAfter(HeartbeatInterval);
		try
		{
			return await reader.WaitToReadAsync(heartbeat.Token);
		}
		catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
		{
			await context.Response.WriteAsync(": ping\n\n", cancellation);
			await context.Response.Body.FlushAsync(cancellation);
			return true;
		}
	}
}
