namespace Core.Playback;

public record PlayAction;
public record PauseAction;

public record PlayNextSongAction;

public record CurrentSongChangedAction(
	Song CurrentSong,
	Song NextSong,
	Song SongOnOtherPlaylist,
	float Gain);

public record SongOnOtherPlaylistChangedAction(Song Song);

public record GainChangedAction(float Gain);

public record SongLoadedAction(Song Song);
