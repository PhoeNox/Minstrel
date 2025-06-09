namespace Features.Playback;

public record LoadSongSignal(Song Song);

public record PlaySongSignal(Song Song, float Gain);
public record PauseSongSignal(Song Song, bool Reset = false);

public record PlaySoundSignal(Song Song, float Gain);

public record SetGainSignal(Song Song, float Gain);

public record SongEndedSignal(Song Song);
public record SongLoadedSignal(Song Song);
