namespace Infrastructure.Network;

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
		var address = NetworkInterface.GetAllNetworkInterfaces()
			.Where(n => n.OperationalStatus == OperationalStatus.Up
			            && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
			.Select(n => n.GetIPProperties())
			.SelectMany(p => p.UnicastAddresses)
			.First(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
		return address.Address.ToString();
	}
}
