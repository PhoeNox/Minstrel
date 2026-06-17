namespace Backend.Sessions;

using System.Threading.Channels;
using Core.Timer;

public sealed class TimerSession
{
	private readonly Lock gate = new();
	private readonly List<Channel<TimerEvent>> subscribers = [];
	private TimerAnchor timer = TimerAnchor.Idle;

	public void Start(TimeSpan duration)
	{
		lock (gate)
		{
			timer = CountdownTimer.Start(duration, Now());
			Broadcast();
		}
	}

	public void Stop()
	{
		lock (gate)
		{
			timer = TimerAnchor.Idle;
			Broadcast();
		}
	}

	public void Tick()
	{
		lock (gate)
		{
			if (!CountdownTimer.HasExpired(timer, Now()))
				return;

			timer = TimerAnchor.Idle;
			Broadcast();
			Publish(new GongEvent());
		}
	}

	public Channel<TimerEvent> Subscribe()
	{
		var channel = Channel.CreateUnbounded<TimerEvent>();
		lock (gate)
		{
			subscribers.Add(channel);
			channel.Writer.TryWrite(CurrentSnapshot());
		}

		return channel;
	}

	public void Unsubscribe(Channel<TimerEvent> channel)
	{
		lock (gate)
		{
			subscribers.Remove(channel);
		}

		channel.Writer.TryComplete();
	}

	private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	private void Broadcast() => Publish(CurrentSnapshot());

	private void Publish(TimerEvent message)
	{
		foreach (var subscriber in subscribers)
		{
			subscriber.Writer.TryWrite(message);
		}
	}

	private TimerSnapshotEvent CurrentSnapshot() => new(timer);
}
