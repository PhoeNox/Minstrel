namespace Player.E2E.Tests;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;

// Runs the real, shipped Backend out-of-process (Kestrel serving the built
// frontends from wwwroot) and a headless Chromium for the whole test assembly.
// Out-of-process keeps the E2E surface identical to what users run; the Backend
// is launched with LaunchBrowser=false so it never pops a browser tab.
public static class BackendServer
{
	private static Process? backend;
	private static IPlaywright? playwright;
	private static readonly ConcurrentQueue<string> Logs = new();

	public static string BaseUrl { get; private set; } = "";
	public static IBrowser Browser { get; private set; } = null!;

	[Before(Assembly)]
	public static async Task StartAsync()
	{
		var repoRoot = FindRepoRoot();
		var port = FreeTcpPort();
		BaseUrl = $"http://127.0.0.1:{port}";

		backend = StartBackend(repoRoot, port);
		await WaitUntilReady(TimeSpan.FromSeconds(90));

		InstallChromium();
		playwright = await Playwright.CreateAsync();
		Browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
	}

	[After(Assembly)]
	public static async Task StopAsync()
	{
		if (Browser is not null)
			await Browser.DisposeAsync();
		playwright?.Dispose();

		if (backend is { HasExited: false })
		{
			backend.Kill(entireProcessTree: true);
			await backend.WaitForExitAsync();
		}
	}

	private static Process StartBackend(string repoRoot, int port)
	{
		var info = new ProcessStartInfo("dotnet")
		{
			WorkingDirectory = repoRoot,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};
		info.ArgumentList.Add("run");
		info.ArgumentList.Add("--project");
		info.ArgumentList.Add(Path.Combine(repoRoot, "Backend", "Backend.csproj"));
		info.ArgumentList.Add("-c");
		info.ArgumentList.Add(Configuration());
		info.Environment["Port"] = port.ToString();
		info.Environment["LaunchBrowser"] = "false";
		info.Environment["Music__Directory"] = Path.Combine(repoRoot, "Backend", "Music");

		var process = Process.Start(info) ?? throw new InvalidOperationException("Failed to start the Backend process.");
		process.OutputDataReceived += (_, e) => { if (e.Data is not null) Logs.Enqueue(e.Data); };
		process.ErrorDataReceived += (_, e) => { if (e.Data is not null) Logs.Enqueue(e.Data); };
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		return process;
	}

	private static async Task WaitUntilReady(TimeSpan timeout)
	{
		using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
		var deadline = DateTime.UtcNow + timeout;
		while (DateTime.UtcNow < deadline)
		{
			if (backend is { HasExited: true })
				throw new InvalidOperationException($"Backend exited early (code {backend.ExitCode}).\n{DrainLogs()}");
			try
			{
				var response = await client.GetAsync($"{BaseUrl}/connection");
				if (response.IsSuccessStatusCode)
					return;
			}
			catch
			{
				// Server not accepting connections yet.
			}
			await Task.Delay(500);
		}
		throw new TimeoutException($"Backend did not become ready within {timeout}.\n{DrainLogs()}");
	}

	private static void InstallChromium()
	{
		var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
		if (exit != 0)
			throw new InvalidOperationException($"Playwright Chromium install failed (exit {exit}).");
	}

	private static string Configuration() =>
		AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}Debug{Path.DirectorySeparatorChar}")
			? "Debug"
			: "Release";

	private static string FindRepoRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Minstrel.slnx")))
			dir = dir.Parent;
		return dir?.FullName ?? throw new InvalidOperationException("Could not locate the repo root (Minstrel.slnx).");
	}

	private static int FreeTcpPort()
	{
		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var port = ((IPEndPoint)listener.LocalEndpoint).Port;
		listener.Stop();
		return port;
	}

	private static string DrainLogs() => string.Join('\n', Logs);
}
