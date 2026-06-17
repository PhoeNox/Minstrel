namespace Backend.Tests.Api;

using System.Text.Json;
using Backend.Api;
using static TestHarness;
using static VerifyTUnit.Verifier;

public class TimerEndpointsTests
{
	[Test]
	public async Task Start_AcceptsCommand()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/timer/start", JsonBody("""{"duration":60}"""));

		await Verify(await StatusAndText(response));
	}

	[Test]
	public async Task Start_RejectsRequest_WhenDurationIsNotPositive()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/timer/start", JsonBody("""{"duration":0}"""));

		await Verify(await StatusAndText(response));
	}

	[Test]
	public async Task Stop_AcceptsCommand()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/timer/stop", content: null);

		await Verify(await StatusAndText(response));
	}

	[Test]
	public async Task Sse_EmitsInitialSnapshot_OnSubscribe()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var data = await Sse.FirstData(client, "/timer/sse");
		var timer = JsonSerializer.Deserialize<TimerDto>(data!, Json);

		await Verify(new { Timer = timer });
	}
}
