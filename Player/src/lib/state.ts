import type { PositionAnchor } from '$shared/position';
import type { TimerAnchor } from '$shared/timer';

export type Phase = 'Day' | 'Night';

export interface SongDto {
	id: string;
	title: string;
	artist: string;
	length: number;
}

export interface PlaylistDto {
	songs: SongDto[];
	currentIndex: number | null;
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
	timer: TimerAnchor | null;
}

const emptyPlaylist: PlaylistDto = { songs: [], currentIndex: null, gain: 1 };

export const emptyState: PlaybackState = {
	phase: 'Day',
	isPlaying: false,
	currentSongId: null,
	playlists: { day: emptyPlaylist, night: emptyPlaylist },
	position: { songId: null, offset: 0, anchorTimestamp: 0, isPlaying: false },
	timer: null
};

export function activePlaylist(state: PlaybackState): PlaylistDto {
	return state.phase === 'Night' ? state.playlists.night : state.playlists.day;
}
