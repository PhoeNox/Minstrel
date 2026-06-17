namespace Backend.Api;

using Core.Connection;
using Core.Version;
using Network;

public static class SystemEndpoints
{
	public static void MapSystemEndpoints(this WebApplication app)
	{
		app.MapGet("/system/connection", GetConnection);
		app.MapGet("/system/version", GetVersion);
	}

	private static IResult GetConnection(INetworkProvider network, IConfiguration configuration)
	{
		var host = ResolveHost(configuration["Host"], network);
		var info = ConnectionInfoFactory.Create(host, configuration["Port"] ?? "5757");
		return Results.Ok(info);
	}

	private static IResult GetVersion() => Results.Ok(VersionReader.Current());

	private static string ResolveHost(string? configuredHost, INetworkProvider network) =>
		string.IsNullOrWhiteSpace(configuredHost)
			? network.GetLocalIpAddress()
			: configuredHost.Trim();
}
