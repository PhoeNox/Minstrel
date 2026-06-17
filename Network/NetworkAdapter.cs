namespace Network;

public record NetworkAdapter(
	bool IsUp,
	bool IsLoopback,
	bool IsVirtual,
	bool OwnsDefaultGateway,
	string[] IpV4Addresses);
