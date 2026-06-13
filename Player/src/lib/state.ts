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
}

export const emptyState: PlaybackState = {
	isPlaying: false,
	currentSongId: null,
	songs: []
};
