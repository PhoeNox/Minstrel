namespace Core.Connection;

// ReSharper disable once NotAccessedPositionalProperty.Global
public record ConnectionInfo(string RemoteUrl);

public static class ConnectionInfoFactory
{
	public static ConnectionInfo Create(string ipAddress, string port)
		=> new($"http://{ipAddress}:{port}/remote");
}
