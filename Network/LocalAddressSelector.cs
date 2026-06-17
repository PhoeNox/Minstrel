namespace Network;

public static class LocalAddressSelector
{
	public static string? Select(IEnumerable<NetworkAdapter> adapters)
	{
		return adapters
				.Where(adapter => adapter is {IsUp: true, IsLoopback: false, IpV4Addresses.Length: > 0})
				.OrderByDescending(Reachability)
				.Select(adapter => adapter.IpV4Addresses[0])
				.FirstOrDefault();
	}

	private static int Reachability(NetworkAdapter adapter)
	{
		var reachability = 0;
		if (adapter.OwnsDefaultGateway)
			reachability += 2;
		if (adapter.IsVirtual is false)
			reachability += 1;
		return reachability;
	}
}
