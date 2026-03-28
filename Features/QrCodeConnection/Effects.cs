namespace Features.QrCodeConnection;

using Infrastructure.Network;

public class Effects(INetworkProvider networkProvider, ConnectionOptions connectionOptions)
{
	[EffectMethod]
	public Task Initialize(StoreInitializedAction action, IDispatcher dispatcher)
	{
		var localIpAddress = networkProvider.GetLocalIpAddress();
		dispatcher.Dispatch(new SetConnectionStringAction($"http://{localIpAddress}:{connectionOptions.Port}/remote"));
		return Task.CompletedTask;
	}
}
