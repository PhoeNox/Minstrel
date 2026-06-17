namespace Core.Version;

using System.Reflection;

public sealed record VersionInfo(string Version);

public static class VersionReader
{
	public static VersionInfo Current()
	{
		var version = ReadInformationalVersion();
		var displayedVersion = Display(version);
		return new VersionInfo(displayedVersion);
	}
	
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
