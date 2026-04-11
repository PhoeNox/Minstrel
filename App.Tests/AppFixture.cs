namespace App.Tests;

using Fluxor;
using Infrastructure.FileSystem;
using Infrastructure.Network;
using Infrastructure.Playback;
using VerifyTests;

public static class AppFixture
{
    public static string BaseUrl { get; private set; } = "";
    private static WebApplication? app;

    [Before(TestSession)]
    public static void InitializeVerify()
    {
        VerifyPlaywright.Initialize();
        PngComparer.Register();
    }

    [Before(TestSession)]
    public static void InstallPlaywright()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
    }

    [Before(TestSession)]
    public static async Task StartServer()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development",
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

        app = builder.Build();

        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App.Components.App>()
            .AddInteractiveServerRenderMode();

        await app.StartAsync();
        BaseUrl = app.Urls.First();
    }

    [After(TestSession)]
    public static async Task StopServer()
    {
        if (app is not null)
            await app.DisposeAsync();
    }
}
