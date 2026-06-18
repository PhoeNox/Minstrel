namespace FileSystem.Tests;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class FileSystemProviderTests
{
	[Test]
	public async Task SkipsUnreadableFileAndReturnsReadableSongs()
	{
		using var music = new TempMusicDirectory();
		music.CopyReadableSong("playable.mp3");
		music.WriteUnreadableFile("corrupt.mp3");

		var logger = new CapturingLogger<FileSystemProvider>();
		var provider = new FileSystemProvider(StaticOptions.For(music.Path), logger);

		var songs = provider.ReadSongs(provider.EnumerateSongs());

		await Assert.That(songs.Select(song => Path.GetFileName(song.Path)))
			.IsEquivalentTo(["playable.mp3"]);
	}

	[Test]
	public async Task LogsSkippedFileWithPathAndReason()
	{
		using var music = new TempMusicDirectory();
		music.CopyReadableSong("playable.mp3");
		music.WriteUnreadableFile("corrupt.mp3");

		var logger = new CapturingLogger<FileSystemProvider>();
		var provider = new FileSystemProvider(StaticOptions.For(music.Path), logger);

		provider.ReadSongs(provider.EnumerateSongs());

		var warning = logger.Entries.Single(entry => entry.Level == LogLevel.Warning);
		await Assert.That(warning.Message).Contains("corrupt.mp3");
		await Assert.That(warning.Exception).IsNotNull();
	}
}

file sealed class TempMusicDirectory : IDisposable
{
	private static readonly string Fixture =
		System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "gong.mp3");

	public string Path { get; } =
		System.IO.Path.Combine(System.IO.Path.GetTempPath(), "minstrel-fs-" + Guid.NewGuid().ToString("N"));

	public TempMusicDirectory()
		=> Directory.CreateDirectory(Path);

	public void CopyReadableSong(string name)
		=> File.Copy(Fixture, System.IO.Path.Combine(Path, name));

	public void WriteUnreadableFile(string name)
		=> File.WriteAllBytes(System.IO.Path.Combine(Path, name), [0x00, 0x01, 0x02, 0x03]);

	public void Dispose()
		=> Directory.Delete(Path, recursive: true);
}

file static class StaticOptions
{
	public static IOptionsMonitor<MusicOptions> For(string directory)
		=> new Monitor(new MusicOptions { Directory = directory });

	private sealed class Monitor(MusicOptions value) : IOptionsMonitor<MusicOptions>
	{
		public MusicOptions CurrentValue => value;
		public MusicOptions Get(string? name) => value;
		public IDisposable? OnChange(Action<MusicOptions, string?> listener) => null;
	}
}

file sealed class CapturingLogger<T> : ILogger<T>
{
	public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

	public bool IsEnabled(LogLevel logLevel) => true;

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
		Func<TState, Exception?, string> formatter)
		=> Entries.Add((logLevel, formatter(state, exception), exception));
}
