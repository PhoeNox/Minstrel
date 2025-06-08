namespace Features.Playlists;

public record LoadPlaylistsAction;
public record PlaylistsLoadedAction;
public record SetPlaylistsAction(Playlist DayPlaylist, Playlist NightPlaylist);

public record SetCurrentSongForDayAction(Song Song);
public record SetCurrentSongForNightAction(Song Song);

public record SetDayGainAction(float Volume);
public record SetNightGainAction(float Volume);
