namespace Backend.Tests.Api;

using System.Net;
using System.Net.Http.Json;

// Direct assertions for the thin endpoints whose only observable result is a status code,
// optionally with a problem message — no structured body worth snapshotting.
public static class HttpAssertions
{
	public static async Task ShouldHaveStatus(this HttpResponseMessage response, HttpStatusCode expected)
		=> await Assert.That(response.StatusCode).IsEqualTo(expected);

	public static async Task ShouldBeProblem(this HttpResponseMessage response, HttpStatusCode expected, string message)
	{
		await Assert.That(response.StatusCode).IsEqualTo(expected);
		// Minimal-API string results are serialized as a JSON string, quotes included.
		await Assert.That(await response.Content.ReadFromJsonAsync<string>()).IsEqualTo(message);
	}
}
