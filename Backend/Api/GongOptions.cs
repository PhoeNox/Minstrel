namespace Backend.Api;

public record GongOptions
{
	public const string Section = "Gong";
	public double Gain { get; init; } = 3;
}
