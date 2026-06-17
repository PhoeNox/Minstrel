namespace Backend.Tests.Network;

using global::Network;

public class VirtualAdapterDetectorTests
{
	[Test]
	[Arguments("VirtualBox Host-Only Network", "Oracle VirtualBox Adapter")]
	[Arguments("vEthernet (WSL)", "Hyper-V Virtual Ethernet Adapter")]
	[Arguments("docker0", "Docker bridge")]
	[Arguments("VPN Tunnel", "TAP-Windows Adapter")]
	public async Task FlagsKnownVirtualAdapters(string name, string description)
	{
		await Assert.That(VirtualAdapterDetector.IsVirtual(name, description)).IsTrue();
	}

	[Test]
	[Arguments("Wi-Fi", "Intel(R) Wireless-AC 9560")]
	[Arguments("Ethernet", "Realtek PCIe GbE Family Controller")]
	public async Task LeavesPhysicalAdaptersUnflagged(string name, string description)
	{
		await Assert.That(VirtualAdapterDetector.IsVirtual(name, description)).IsFalse();
	}
}
