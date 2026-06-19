namespace Backend.Sessions;

using System.Threading.Channels;

// Non-thread-safe SSE hub: callers must serialize access through their own
// coordinating lock (ADR-0007), so a new subscriber's initial snapshot stays
// consistent with the broadcast stream. A lock here would split that atomicity.
//
// The per-subscriber channel policy is chosen per broadcaster instance: a stream
// of coalesceable snapshots can use a bounded DropOldest channel to cap a stalled
// reader, while a stream carrying must-deliver one-shots stays unbounded.
public sealed class SseBroadcaster<TEvent>(Func<Channel<TEvent>> createChannel)
{
	private readonly List<Channel<TEvent>> subscribers = [];

	public SseBroadcaster() : this(Channel.CreateUnbounded<TEvent>) { }

	public Channel<TEvent> Add()
	{
		var channel = createChannel();
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
