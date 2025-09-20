namespace Features.QrCodeConnection;

using Infrastructure.Network;

public class Effects(INetworkProvider networkProvider)
{
	[EffectMethod]
	public Task Initialize(StoreInitializedAction action, IDispatcher dispatcher)
	{
		var localIpAddress = networkProvider.GetLocalIpAddress();
		dispatcher.Dispatch(new SetConnectionStringAction($"http://{localIpAddress}:5000/remote"));
		return Task.CompletedTask;
	}
}
