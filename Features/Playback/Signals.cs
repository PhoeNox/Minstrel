namespace Features.Playback;

public record LoadSongSignal(Song Song);

public record PlaySignal(Song Song, float Gain);
public record PauseSignal(Song Song, bool Reset = false);

public record SetGainSignal(Song Song, float Gain);

public record SongEndedSignal(Song Song);
public record SongLoadedSignal(Song Song);
