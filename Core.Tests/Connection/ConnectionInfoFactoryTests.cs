namespace Core.Tests.Connection;

using Core.Connection;

public class ConnectionInfoFactoryTests
{
	[Test]
	public async Task BuildsTheRemoteUrlFromIpAndPort()
	{
		var info = ConnectionInfoFactory.Create("192.168.1.42", "5000");
		await Assert.That(info.RemoteUrl).IsEqualTo("http://192.168.1.42:5000/remote");
	}
}
