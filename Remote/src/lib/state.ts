import type { PositionAnchor } from '$shared/position';

export interface SongDto {
	id: string;
	title: string;
	artist: string;
	length: number;
}

export interface PlaybackState {
	isPlaying: boolean;
	currentSongId: string | null;
	songs: SongDto[];
	position: PositionAnchor;
}

export const emptyState: PlaybackState = {
	isPlaying: false,
	currentSongId: null,
	songs: [],
	position: { songId: null, offset: 0, anchorTimestamp: 0, isPlaying: false }
};
