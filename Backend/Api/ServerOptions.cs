namespace Backend.Api;

public static class ServerOptions
{
	public const string PortKey = "Port";
	public const string DefaultPort = "5757";

	public static string ResolvePort(this IConfiguration configuration) =>
		configuration[PortKey] ?? DefaultPort;
}
