namespace Features.Playlists;

public record LoadPlaylistsAction;
public record PlaylistsLoadedAction;
public record SetPlaylistsAction(GamePhase GamePhase, Song[] Songs);
public record SetDatabaseAction(Song[] Songs);

public record AddSongToPlaylistAction(GamePhase GamePhase, Song Song);
public record MoveSongAction(GamePhase GamePhase, int OldIndex, int NewIndex);

public record SetCurrentSongAction(GamePhase GamePhase, Song Song, Song? PreviousSong = null);

public record SetGainAction(GamePhase GamePhase, float Volume);
