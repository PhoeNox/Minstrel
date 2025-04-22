namespace App;

public class AppState
{
	public Core.Playlists Playlists { get; set; } = new(new Playlist([]), new Playlist([]));
	
	public GamePhase GamePhase { get; set; } = GamePhase.Day;
	
	public Song? CurrentSong { get; set; }
	public Song? NextSong { get; set; }
	public Song? SongOnOtherPlaylist { get; set; }

	public float DayGain { get; set; } = 1;
	public float NightGain { get; set; } = 1;
}
