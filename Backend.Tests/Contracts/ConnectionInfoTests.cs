namespace Backend.Tests.Contracts;

using Backend.Contracts;

public class ConnectionInfoTests
{
	[Test]
	public async Task BuildsTheRemoteUrlFromIpAndPort()
	{
		var info = ConnectionInfo.For("192.168.1.42", "5000");

		await Assert.That(info.RemoteUrl).IsEqualTo("http://192.168.1.42:5000/remote");
	}
}
