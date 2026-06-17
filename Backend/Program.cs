using System.Diagnostics;
using System.Text.Json.Serialization;
using Backend.Api;
using Backend.Features.Library;
using Backend.Features.Playback;
using Backend.Features.Session;
using Backend.Features.Timer;
using Core.Library;
using FileSystem;
using Network;

var builder = WebApplication.CreateBuilder(args);

var port = builder.Configuration["Port"] ?? "5757";
builder.WebHost.UseUrls($"http://*:{port}");

builder.Services.AddOptions<MusicOptions>()
	.Bind(builder.Configuration.GetSection(MusicOptions.Section))
	.ValidateOnStart();

builder.Services.AddOptions<GongOptions>()
	.Bind(builder.Configuration.GetSection(GongOptions.Section));

builder.Services.AddSingleton<IFileSystemProvider, FileSystemProvider>();
builder.Services.AddSingleton<INetworkProvider, NetworkProvider>();
builder.Services.AddSingleton<SongPool>();
builder.Services.AddSingleton<LiveSession>();
builder.Services.AddHostedService<PlaybackClock>();
builder.Services.AddSingleton<TimerSession>();
builder.Services.AddHostedService<TimerClock>();

builder.Services.ConfigureHttpJsonOptions(options =>
	options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapSystemEndpoints();
app.MapLibraryEndpoints();
app.MapPlaylistEndpoints();
app.MapPlaybackEndpoints();
app.MapTimerEndpoints();
app.MapFallbackToFile("remote/{*path:nonfile}", "remote/index.html");
app.MapFallbackToFile("index.html");

var playerUrl = $"http://localhost:{port}";
var appTask = app.RunAsync();
if (builder.Configuration.GetValue("LaunchBrowser", true))
	OpenPlayerInBrowser(playerUrl);
await appTask;

return;

static void OpenPlayerInBrowser(string playerUrl)
{
	var openPlayer = new ProcessStartInfo
	{
		FileName = playerUrl,
		UseShellExecute = true,
	};
	try
	{
		Process.Start(openPlayer);
	}
	catch
	{
		// Headless host (CI, server, E2E): no browser to launch.
	}
}
