namespace Network.Tests;

public class LocalAddressSelectorTests
{
	[Test]
	public async Task PrefersTheGatewayOwningAdapterOverAVirtualOneWithoutAGateway()
	{
		var virtualAdapter = Adapter("VirtualBox Host-Only", "192.168.56.1", isVirtual: true, ownsGateway: false);
		var wifi = Adapter("Wi-Fi", "192.168.1.42", isVirtual: false, ownsGateway: true);

		var selected = LocalAddressSelector.Select([virtualAdapter, wifi]);

		await Assert.That(selected).IsEqualTo("192.168.1.42");
	}

	[Test]
	public async Task PrefersTheNonVirtualAdapterWhenBothOwnAGateway()
	{
		var vpn = Adapter("VPN", "10.8.0.2", isVirtual: true, ownsGateway: true);
		var ethernet = Adapter("Ethernet", "192.168.1.10", isVirtual: false, ownsGateway: true);

		var selected = LocalAddressSelector.Select([vpn, ethernet]);

		await Assert.That(selected).IsEqualTo("192.168.1.10");
	}

	[Test]
	public async Task SkipsLoopbackAndDownAdapters()
	{
		var loopback = new NetworkAdapter(IsUp: true, IsLoopback: true, IsVirtual: false,
			OwnsDefaultGateway: false, IpV4Addresses: ["127.0.0.1"]);
		var down = new NetworkAdapter(IsUp: false, IsLoopback: false, IsVirtual: false,
			OwnsDefaultGateway: true, IpV4Addresses: ["192.168.1.5"]);
		var wifi = Adapter("Wi-Fi", "192.168.1.42", isVirtual: false, ownsGateway: true);

		var selected = LocalAddressSelector.Select([loopback, down, wifi]);

		await Assert.That(selected).IsEqualTo("192.168.1.42");
	}

	[Test]
	public async Task ReturnsNullWhenNoUsableAdapterExists()
	{
		var loopback = new NetworkAdapter(IsUp: true, IsLoopback: true, IsVirtual: false,
			OwnsDefaultGateway: false, IpV4Addresses: ["127.0.0.1"]);

		var selected = LocalAddressSelector.Select([loopback]);

		await Assert.That(selected).IsNull();
	}

	private static NetworkAdapter Adapter(string name, string ipv4, bool isVirtual, bool ownsGateway) =>
		new(IsUp: true, IsLoopback: false, IsVirtual: isVirtual,
			OwnsDefaultGateway: ownsGateway, IpV4Addresses: [ipv4]);
}
