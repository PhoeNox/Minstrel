namespace Network;

public static class VirtualAdapterDetector
{
	private static readonly string[] Markers =
	[
		"virtual", "vmware", "vbox", "virtualbox", "hyper-v", "hyperv",
		"vethernet", "docker", "wsl", "vpn", "tap", "tunnel", "pseudo",
	];

	public static bool IsVirtual(string name, string description) =>
		Markers.Any(marker => Mentions(name, marker) || Mentions(description, marker));

	private static bool Mentions(string value, string marker) =>
		value.Contains(marker, StringComparison.OrdinalIgnoreCase);
}
