namespace Backend.Tests.Helpers;

using Network;

public sealed class FakeNetwork(string address) : INetworkProvider
{
	public string GetLocalIpAddress() => address;
}
