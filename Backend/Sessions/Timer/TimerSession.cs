namespace Backend.Sessions.Timer;

using System.Threading.Channels;
using Core.Timer;

public sealed class TimerSession
{
	private readonly Lock gate = new();
	private readonly SseBroadcaster<TimerEvent> broadcaster = new();
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
			broadcaster.Publish(new GongEvent());
		}
	}

	public Channel<TimerEvent> Subscribe()
	{
		lock (gate)
		{
			var channel = broadcaster.Add();
			channel.Writer.TryWrite(CurrentSnapshot());
			return channel;
		}
	}

	public void Unsubscribe(Channel<TimerEvent> channel)
	{
		lock (gate)
		{
			broadcaster.Remove(channel);
		}
	}

	private static long Now() 
		=> DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	private void Broadcast() 
		=> broadcaster.Publish(CurrentSnapshot());

	private TimerSnapshotEvent CurrentSnapshot() 
		=> new(timer);
}
