using App.Playback;
using Fluxor;
using Infrastructure.FileSystem;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

var urls = builder.Configuration["Urls"];
if (!string.IsNullOrEmpty(urls))
	builder.WebHost.UseUrls(urls);

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

builder.Services.AddScoped<PlaybackService>();
builder.Services.AddSingleton<IPlaylistLoader, PlaylistLoader>();

var coreAssembly = typeof(Song).Assembly;
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

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App.Components.App>()
	.AddInteractiveServerRenderMode();

await app.RunAsync();
