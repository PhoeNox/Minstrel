import type { PositionAnchor } from '$shared/position';

export type Phase = 'Day' | 'Night';

export interface SongDto {
	id: string;
	title: string;
	artist: string;
	length: number;
}

export interface PlaylistDto {
	songs: SongDto[];
	currentSongId: string | null;
	gain: number;
}

export interface PlaylistsDto {
	day: PlaylistDto;
	night: PlaylistDto;
}

export interface PlaybackState {
	phase: Phase;
	isPlaying: boolean;
	currentSongId: string | null;
	playlists: PlaylistsDto;
	position: PositionAnchor;
}

const emptyPlaylist: PlaylistDto = { songs: [], currentSongId: null, gain: 1 };

export const emptyState: PlaybackState = {
	phase: 'Day',
	isPlaying: false,
	currentSongId: null,
	playlists: { day: emptyPlaylist, night: emptyPlaylist },
	position: { songId: null, offset: 0, anchorTimestamp: 0, isPlaying: false }
};

export function activePlaylist(state: PlaybackState): PlaylistDto {
	return state.phase === 'Night' ? state.playlists.night : state.playlists.day;
}
