using App.Tests;

[assembly: SkipOnCi]

namespace App.Tests;

public class SkipOnCiAttribute() : SkipAttribute("This test is skipped on CI")
{
	public override Task<bool> ShouldSkip(TestRegisteredContext context)
	{
		var isCi = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
		return Task.FromResult(isCi);
	}
}
