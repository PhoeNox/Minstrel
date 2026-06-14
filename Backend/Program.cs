using System.Diagnostics;
using System.Text.Json.Serialization;
using Backend.Endpoints;
using Backend.Library;
using Backend.Playback;
using Infrastructure.FileSystem;
using Infrastructure.Network;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var port = builder.Configuration["Port"] ?? "5757";
var urlsFromHost = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (string.IsNullOrEmpty(urlsFromHost))
	builder.WebHost.UseUrls($"http://*:{port}");

builder.Services.AddOptions<MusicOptions>()
	.Bind(builder.Configuration.GetSection(MusicOptions.Section))
	.ValidateOnStart();

builder.Services.AddSingleton<IFileSystemProvider, FileSystemProvider>();
builder.Services.AddSingleton<INetworkProvider, NetworkProvider>();
builder.Services.AddSingleton<SongLibrary>();
builder.Services.AddSingleton<PlaybackSession>();
builder.Services.AddHostedService<PlaybackClock>();

builder.Services.ConfigureHttpJsonOptions(options =>
	options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPlaybackEndpoints();
app.MapFallbackToFile("remote/{*path}", "remote/index.html");
app.MapFallbackToFile("index.html");

var playerUrl = $"http://localhost:{port}";
var appTask = app.RunAsync();
if (builder.Configuration.GetValue("OpenPlayer", true))
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
	Process.Start(openPlayer);
}
