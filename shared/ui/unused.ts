import type { PlaylistsDto, SongDto } from './state';

export function unusedSongs(library: SongDto[], playlists: PlaylistsDto): SongDto[] {
	const usedIds = new Set(
		[...playlists.day.songs, ...playlists.night.songs].map((song) => song.id)
	);
	return library.filter((song) => !usedIds.has(song.id));
}
