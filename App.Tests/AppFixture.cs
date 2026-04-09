using Fluxor;
using Infrastructure.FileSystem;
using Infrastructure.Network;
using Infrastructure.Playback;
using TUnit.Core;

namespace App.Tests;

public static class AppFixture
{
    public static string BaseUrl { get; private set; } = "";
    static WebApplication? _app;

    public static readonly string ScreenshotsDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "screenshots"));

    [Before(HookType.TestSession)]
    public static async Task StartServer()
    {
        Directory.CreateDirectory(ScreenshotsDir);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });

        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddBlazorBootstrap();
        builder.Services.AddScoped<PlaybackService>();
        builder.Services.AddSingleton<IFileSystemProvider, FileSystemProvider>();
        builder.Services.AddSingleton<INetworkProvider, NetworkProvider>();
        builder.Services.AddSingleton(new ConnectionOptions("0"));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddOptions<MusicOptions>()
            .Bind(builder.Configuration.GetSection(MusicOptions.Section));

        var coreAssembly = typeof(Features.Playback.State).Assembly;
        builder.Services.AddFluxor(options => options
            .ScanAssemblies(coreAssembly)
            .WithLifetime(StoreLifetime.Singleton));

        _app = builder.Build();

        _app.UseAntiforgery();
        _app.MapStaticAssets();
        _app.MapRazorComponents<App.Components.App>()
            .AddInteractiveServerRenderMode();

        await _app.StartAsync();
        BaseUrl = _app.Urls.First();
    }

    [After(HookType.TestSession)]
    public static async Task StopServer()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }
}
