namespace Features.Playback;

public record SwitchPlaylist(GamePhase OldGamePhase, GamePhase NewGamePhase);

public record CurrentSongChangedAction(
	Song CurrentSong,
	Song NextSong,
	Song SongOnOtherPlaylist,
	float Gain);

public record GainChangedAction(float Gain);
