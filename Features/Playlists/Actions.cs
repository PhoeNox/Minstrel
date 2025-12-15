namespace Features.Playlists;

public record LoadPlaylistsAction;
public record PlaylistsLoadedAction;
public record SetPlaylistsAction(Song[] DaySongs, Song[] NightSongs);

public record MoveDaySongAction(int OldIndex, int NewIndex);
public record MoveNightSongAction(int OldIndex, int NewIndex);

public record SetCurrentSongForDayAction(Song Song, Song? PreviousSong = null);
public record SetCurrentSongForNightAction(Song Song, Song? PreviousSong = null);

public record SetDayGainAction(float Volume);
public record SetNightGainAction(float Volume);
