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

	[Test]
	public async Task BuildsADevServerRemoteUrlWithoutThePublishedRemotePath()
	{
		var info = ConnectionInfo.ForDevServer("192.168.1.42", "34512");

		await Assert.That(info.RemoteUrl).IsEqualTo("http://192.168.1.42:34512");
	}
}
