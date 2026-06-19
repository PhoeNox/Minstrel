namespace Backend.Api;

using System.Threading.Channels;

internal static class SsePump
{
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
			await foreach (var message in channel.Reader.ReadAllAsync(cancellation))
			{
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
}
