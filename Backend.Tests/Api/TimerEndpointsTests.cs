namespace Backend.Tests.Api;

using System.Net;
using static TestHarness;

public class TimerEndpointsTests
{
	[Test]
	public async Task Start_AcceptsCommand()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/timer/start", JsonBody("""{"duration":60}"""));

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
	}

	[Test]
	public async Task Start_RejectsRequest_WhenDurationIsNotPositive()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/timer/start", JsonBody("""{"duration":0}"""));

		await response.ShouldBeProblem(HttpStatusCode.BadRequest, "A positive duration is required.");
	}

	[Test]
	public async Task Stop_AcceptsCommand()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var response = await client.PostAsync("/timer/stop", content: null);

		await response.ShouldHaveStatus(HttpStatusCode.NoContent);
	}

	[Test]
	public async Task Sse_EmitsIdleSnapshot_OnSubscribe()
	{
		await using var app = new MinstrelApp(SeededLibrary());
		var client = app.CreateClient();

		var data = await Sse.FirstData(client, "/timer/sse");

		await Assert.That(data).IsEqualTo("null");
	}
}
