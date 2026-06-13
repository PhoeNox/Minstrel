using System.Diagnostics;
using Backend.Endpoints;
using Backend.Library;
using Backend.Playback;
using Infrastructure.FileSystem;
using Infrastructure.Network;

var builder = WebApplication.CreateBuilder(args);

var port = builder.Configuration["Port"] ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

builder.Services.AddOptions<MusicOptions>()
	.Bind(builder.Configuration.GetSection(MusicOptions.Section))
	.ValidateOnStart();

builder.Services.AddSingleton<IFileSystemProvider, FileSystemProvider>();
builder.Services.AddSingleton<INetworkProvider, NetworkProvider>();
builder.Services.AddSingleton<SongLibrary>();
builder.Services.AddSingleton<PlaybackSession>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPlaybackEndpoints();
app.MapFallbackToFile("index.html");

var playerUrl = $"http://localhost:{port}";
var appTask = app.RunAsync();
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
