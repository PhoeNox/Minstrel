import { derivePosition } from '$shared/core';
import { activePlaylist, inactivePlaylist, type PlaybackState, type PlaylistDto } from './state';

export const FADE_SECONDS = 5;

export type AudioOperation =
	| { type: 'play'; songId: string; offset: number; gain: number }
	| { type: 'set-gain'; gain: number }
	| { type: 'stop' }
	| { type: 'prefetch'; songId: string };

export interface AudioGraph {
	leadSongId: string | null;
	leadPosition: number | null;
	leadGain: number | null;
	loadedSongIds: string[];
}

export function reconcile(desired: PlaybackState, current: AudioGraph, now: number): AudioOperation[] {
	const wanted = desired.isPlaying ? desired.currentSongId : null;
	if (wanted === null) {
		return current.leadSongId === null ? [] : [{ type: 'stop' }];
	}

	const gain = activePlaylist(desired).gain;
	const next = nextSongId(desired);
	const operations: AudioOperation[] = [];

	const anticipated = next !== null && current.leadSongId === next;
	if (current.leadSongId !== wanted && !anticipated) {
		operations.push({ type: 'play', songId: wanted, offset: derivePosition(desired.position, now), gain });
	} else if (current.leadSongId === wanted && nearingEnd(desired, current, wanted, next)) {
		operations.push({ type: 'play', songId: next as string, offset: 0, gain });
	} else if (gain !== current.leadGain) {
		operations.push({ type: 'set-gain', gain });
	}

	for (const songId of warmTargets(desired, wanted, next)) {
		if (!current.loadedSongIds.includes(songId)) {
			operations.push({ type: 'prefetch', songId });
		}
	}

	return operations;
}

// The two buffers worth warming ahead of need: the active playlist's next song
// (auto-advance) and the inactive phase's Current Entry (so a phase switch can
// crossfade at once instead of stalling on a cold fetch + decode). Prefetch ops
// follow the play/gain op so a switch begins fading immediately and the warming
// loads behind it.
function warmTargets(desired: PlaybackState, wanted: string, next: string | null): string[] {
	const targets: string[] = [];
	if (next !== null && next !== wanted) {
		targets.push(next);
	}
	const otherCurrent = currentSongId(inactivePlaylist(desired));
	if (otherCurrent !== null && otherCurrent !== wanted && !targets.includes(otherCurrent)) {
		targets.push(otherCurrent);
	}
	return targets;
}

function currentSongId(playlist: PlaylistDto): string | null {
	const index = playlist.currentIndex;
	if (index === null || index < 0 || index >= playlist.songs.length) {
		return null;
	}
	return playlist.songs[index].id;
}

function nearingEnd(
	desired: PlaybackState,
	current: AudioGraph,
	songId: string,
	next: string | null
): boolean {
	const length = songLength(desired, songId);
	return (
		next !== null &&
		current.loadedSongIds.includes(next) &&
		current.leadPosition !== null &&
		length !== null &&
		length > FADE_SECONDS &&
		current.leadPosition >= length - FADE_SECONDS
	);
}

function nextSongId(desired: PlaybackState): string | null {
	const songs = activePlaylist(desired).songs;
	if (desired.currentSongId === null || songs.length === 0) {
		return null;
	}

	const index = songs.findIndex((song) => song.id === desired.currentSongId);
	return index < 0 ? null : songs[(index + 1) % songs.length].id;
}

function songLength(desired: PlaybackState, songId: string): number | null {
	const song = activePlaylist(desired).songs.find((entry) => entry.id === songId);
	return song ? song.length : null;
}
