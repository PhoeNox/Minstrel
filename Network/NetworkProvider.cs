namespace Network;

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

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
				IsUp: nic.OperationalStatus == OperationalStatus.Up,
				IsLoopback: nic.NetworkInterfaceType == NetworkInterfaceType.Loopback,
				IsVirtual: VirtualAdapterDetector.IsVirtual(nic.Name, nic.Description),
				OwnsDefaultGateway: OwnsDefaultGateway(properties),
				IpV4Addresses: IpV4Addresses(properties));
	}

	private static bool OwnsDefaultGateway(IPInterfaceProperties properties)
	{
		return properties.GatewayAddresses
				.Any(gateway => gateway.Address.AddressFamily == AddressFamily.InterNetwork
				                && !gateway.Address.Equals(IPAddress.Any));
	}

	private static string[] IpV4Addresses(IPInterfaceProperties properties)
	{
		return properties.UnicastAddresses
				.Where(unicast => unicast.Address.AddressFamily == AddressFamily.InterNetwork)
				.Select(unicast => unicast.Address.ToString())
				.ToArray();
	}
}
