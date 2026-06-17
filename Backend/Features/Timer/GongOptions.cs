namespace Backend.Features.Timer;

public record GongOptions
{
	public const string Section = "Gong";
	public double Gain { get; init; } = 3;
}
