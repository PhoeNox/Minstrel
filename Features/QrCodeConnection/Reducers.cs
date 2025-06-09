namespace Features.QrCodeConnection;

public class Reducers
{
	[ReducerMethod]
	public static State ShowQrCode(State state, ShowQrCodeAction action)
		=> state with {QrCodeVisibility = QrCodeVisibility.Visible};

	[ReducerMethod]
	public static State HideQrCode(State state, HideQrCodeAction action)
		=> state with {QrCodeVisibility = QrCodeVisibility.Hidden};

	[ReducerMethod]
	public static State SetConnectionString(State state, SetConnectionStringAction action)
	{
		var newState = state with {ConnectionString = action.ConnectionString};
		if (newState.QrCodeVisibility == QrCodeVisibility.ShowWhenConnectionStringAvailable)
			newState = newState with {QrCodeVisibility = QrCodeVisibility.Visible};
		return newState;
	}
}
