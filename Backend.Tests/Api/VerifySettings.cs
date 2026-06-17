namespace Backend.Tests.Api;

using System.Runtime.CompilerServices;

public static class VerifySettings
{
	[ModuleInitializer]
	public static void Init()
	{
		// Drop the two clock-derived fields from snapshots: a position anchor and the
		// assembly version are wall-clock / build dependent and carry no wiring signal.
		VerifierSettings.IgnoreMembers("AnchorTimestamp", "Version");
	}
}
