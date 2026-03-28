using System.Diagnostics;
using Fluxor;
using Infrastructure.FileSystem;
using Infrastructure.Network;
using Infrastructure.Playback;

var builder = WebApplication.CreateBuilder(args);

var port = builder.Configuration["Port"]!;
builder.WebHost.UseUrls($"http://*:{port}");

builder.Services.AddOptions<MusicOptions>()
		.Bind(builder.Configuration.GetSection(MusicOptions.Section))
		.ValidateOnStart();

builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddBlazorBootstrap();

builder.Services.AddScoped<PlaybackService>();
builder.Services.AddSingleton<IFileSystemProvider, FileSystemProvider>();
builder.Services.AddSingleton<INetworkProvider, NetworkProvider>();
builder.Services.AddSingleton(new ConnectionOptions(port));
builder.Services.AddSingleton(TimeProvider.System);

var coreAssembly = typeof(Features.Playback.State).Assembly;
builder.Services.AddFluxor(options => options
	.ScanAssemblies(coreAssembly)
	.WithLifetime(StoreLifetime.Singleton));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App.Components.App>()
	.AddInteractiveServerRenderMode();

var appTask = app.RunAsync();
OpenPlayerInBrowser($"http://botc.localhost:{port}");
await appTask;

void OpenPlayerInBrowser(string playerUrl)
{
	var openPlayer = new ProcessStartInfo
	{
			FileName = playerUrl,
			UseShellExecute = true,
	};
	Process.Start(openPlayer);
}
