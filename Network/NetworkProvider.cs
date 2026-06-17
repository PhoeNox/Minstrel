namespace Network;

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public record ConnectionOptions(string Port);

public interface INetworkProvider
{
	string GetLocalIpAddress();
}

public class NetworkProvider : INetworkProvider
{
	public string GetLocalIpAddress()
	{
		var adapters = NetworkInterface.GetAllNetworkInterfaces().Select(Describe);
		return LocalAddressSelector.Select(adapters) ?? IPAddress.Loopback.ToString();
	}

	private static NetworkAdapter Describe(NetworkInterface nic)
	{
		var properties = nic.GetIPProperties();
		return new NetworkAdapter(
			Name: nic.Name,
			IsUp: nic.OperationalStatus == OperationalStatus.Up,
			IsLoopback: nic.NetworkInterfaceType == NetworkInterfaceType.Loopback,
			IsVirtual: VirtualAdapterDetector.IsVirtual(nic.Name, nic.Description),
			OwnsDefaultGateway: OwnsDefaultGateway(properties),
			IPv4Addresses: IPv4Addresses(properties));
	}

	private static bool OwnsDefaultGateway(IPInterfaceProperties properties) =>
		properties.GatewayAddresses.Any(gateway =>
			gateway.Address.AddressFamily == AddressFamily.InterNetwork
			&& !gateway.Address.Equals(IPAddress.Any));

	private static IReadOnlyList<string> IPv4Addresses(IPInterfaceProperties properties) =>
		properties.UnicastAddresses
			.Where(unicast => unicast.Address.AddressFamily == AddressFamily.InterNetwork)
			.Select(unicast => unicast.Address.ToString())
			.ToArray();
}
