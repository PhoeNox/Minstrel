namespace Backend.Tests.Helpers;

// Reads the first SSE frame from a stream endpoint. Both stream endpoints push the current
// snapshot the instant a subscriber attaches, so the first `data:` line is that snapshot;
// the reader takes it and lets the request dispose, closing the long-lived stream.
public static class Sse
{
	public static async Task<string?> FirstData(HttpClient client, string url)
	{
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		using var response = await client.SendAsync(
			request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
		await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
		using var reader = new StreamReader(stream);

		while (await reader.ReadLineAsync(timeout.Token) is { } line)
		{
			if (line.StartsWith("data: "))
				return line["data: ".Length..];
		}

		return null;
	}
}
