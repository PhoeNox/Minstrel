namespace Backend.Tests.Library;

using Core;

public class SongIdTests
{
	[Test]
	public async Task DerivesAStableSixteenCharLowercaseHexId()
	{
		var id = SongId.From("/music/a.mp3");

		await Assert.That(id).IsEqualTo(SongId.From("/music/a.mp3"));
		await Assert.That(id).Matches("^[0-9a-f]{16}$");
	}

	[Test]
	public async Task DerivesDistinctIdsForDistinctPaths()
	{
		await Assert.That(SongId.From("a.mp3")).IsNotEqualTo(SongId.From("b.mp3"));
	}
}
