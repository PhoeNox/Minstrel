namespace Backend.Sessions;

using System.Threading.Channels;

// Non-thread-safe SSE hub: callers must serialize access through their own
// coordinating lock (ADR-0007), so a new subscriber's initial snapshot stays
// consistent with the broadcast stream. A lock here would split that atomicity.
public sealed class SseBroadcaster<TEvent>
{
	private readonly List<Channel<TEvent>> subscribers = [];

	public Channel<TEvent> Add()
	{
		var channel = Channel.CreateUnbounded<TEvent>();
		subscribers.Add(channel);
		return channel;
	}

	public void Remove(Channel<TEvent> channel)
	{
		subscribers.Remove(channel);
		channel.Writer.TryComplete();
	}

	public void Publish(TEvent message)
	{
		foreach (var subscriber in subscribers)
		{
			subscriber.Writer.TryWrite(message);
		}
	}
}
