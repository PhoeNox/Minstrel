namespace Backend.Contracts;

using System.Reflection;

public sealed record VersionInfo(string Version)
{
	public static VersionInfo Current { get; } = new(Display(ReadInformationalVersion()));

	private static string ReadInformationalVersion() =>
		Assembly.GetEntryAssembly()?
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
			.InformationalVersion ?? "0.0.0";

	private static string Display(string informational)
	{
		var metadata = informational.IndexOf('+');
		return metadata < 0 ? informational : informational[..metadata];
	}
}
