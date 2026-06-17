namespace Network;

public static class LocalAddressSelector
{
	public static string? Select(IEnumerable<NetworkAdapter> adapters) =>
		adapters
			.Where(adapter => adapter.IsUp && !adapter.IsLoopback && adapter.IPv4Addresses.Count > 0)
			.OrderByDescending(Reachability)
			.Select(adapter => adapter.IPv4Addresses[0])
			.FirstOrDefault();

	private static int Reachability(NetworkAdapter adapter) =>
		(adapter.OwnsDefaultGateway ? 2 : 0) + (adapter.IsVirtual ? 0 : 1);
}
