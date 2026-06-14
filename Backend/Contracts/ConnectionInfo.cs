namespace Backend.Contracts;

public sealed record ConnectionInfo(string RemoteUrl)
{
	public static ConnectionInfo For(string localIpAddress, string port) =>
		new($"http://{localIpAddress}:{port}/remote");

	public static ConnectionInfo ForDevServer(string localIpAddress, string port) =>
		new($"http://{localIpAddress}:{port}");
}
