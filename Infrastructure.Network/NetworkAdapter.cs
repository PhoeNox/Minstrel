namespace Infrastructure.Network;

public sealed record NetworkAdapter(
	string Name,
	bool IsUp,
	bool IsLoopback,
	bool IsVirtual,
	bool OwnsDefaultGateway,
	IReadOnlyList<string> IPv4Addresses);
