namespace Features.Playlists;

public record LoadPlaylistsAction;
public record PlaylistsLoadedAction;
public record SetPlaylistsAction(GamePhase GamePhase, Song[] Songs);
public record SetDatabaseAction(Song[] Songs);

public record AddSongToPlaylistAction(GamePhase GamePhase, Song Song);

public record MoveDaySongAction(int OldIndex, int NewIndex);
public record MoveNightSongAction(int OldIndex, int NewIndex);

public record SetCurrentSongForDayAction(Song Song, Song? PreviousSong = null);
public record SetCurrentSongForNightAction(Song Song, Song? PreviousSong = null);

public record SetGainAction(GamePhase GamePhase, float Volume);
