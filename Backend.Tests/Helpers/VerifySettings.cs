namespace Backend.Tests.Helpers;

using System.Runtime.CompilerServices;

public static class VerifySettings
{
	[ModuleInitializer]
	public static void Init()
	{
		// The position anchor is a wall-clock timestamp; drop it so the state snapshot stays
		// stable across runs.
		VerifierSettings.IgnoreMembers("AnchorTimestamp");
	}
}
