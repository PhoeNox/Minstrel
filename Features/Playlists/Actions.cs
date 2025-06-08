namespace Features.Playlists;

public record LoadPlaylistsAction;
public record PlaylistsLoadedAction;
public record SetPlaylistsAction(Song[] daySongs, Song[] nightSongs);

public record SetCurrentSongForDayAction(Song Song);
public record SetCurrentSongForNightAction(Song Song);

public record SetDayGainAction(float Volume);
public record SetNightGainAction(float Volume);
