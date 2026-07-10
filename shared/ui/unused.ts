import type { PlaylistsDto, SongDto } from './state';

export function unusedSongs<T extends SongDto>(library: T[], playlists: PlaylistsDto): T[] {
	const usedIds = new Set(
		[...playlists.day.songs, ...playlists.night.songs].map((song) => song.id)
	);
	return library.filter((song) => !usedIds.has(song.id));
}
