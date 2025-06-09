namespace Features.QrCodeConnection;

[FeatureState]
public record State
{
	public QrCodeVisibility QrCodeVisibility { get; init; } = QrCodeVisibility.ShowWhenConnectionStringAvailable;
	public string? ConnectionString { get; init; }
}

public enum QrCodeVisibility
{
	ShowWhenConnectionStringAvailable,
	Hidden,
	Visible,
}
