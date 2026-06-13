import type { PlaybackState } from './state';

export type AudioOperation =
	| { type: 'start'; songId: string }
	| { type: 'stop'; songId: string };

export interface AudioGraph {
	playingSongId: string | null;
}

export function reconcile(desired: PlaybackState, current: AudioGraph): AudioOperation[] {
	const wanted = desired.isPlaying ? desired.currentSongId : null;
	const operations: AudioOperation[] = [];

	if (current.playingSongId !== null && current.playingSongId !== wanted) {
		operations.push({ type: 'stop', songId: current.playingSongId });
	}

	if (wanted !== null && wanted !== current.playingSongId) {
		operations.push({ type: 'start', songId: wanted });
	}

	return operations;
}
