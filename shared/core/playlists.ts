// Pure playlist ordering transforms — the TS mirror of C# `Core.Playlist.Playlists`.
// Holds no cursor, gain, or position; operates only on the ordered entry array.

import type { PlaylistEntry } from './types';

// A source of randomness in [0, 1), injected so `shuffle` is deterministic in tests.
export type Rng = () => number;

export function indexOfSong(entries: PlaylistEntry[], songId: string): number | null {
	const index = entries.findIndex((entry) => entry.id === songId);
	return index < 0 ? null : index;
}

export function add(entries: PlaylistEntry[], entry: PlaylistEntry): PlaylistEntry[] {
	return [...entries, entry];
}

export function removeAt(entries: PlaylistEntry[], index: number): PlaylistEntry[] {
	if (index < 0 || index >= entries.length) {
		return entries;
	}
	return [...entries.slice(0, index), ...entries.slice(index + 1)];
}

export function move(entries: PlaylistEntry[], oldIndex: number, newIndex: number): PlaylistEntry[] {
	if (oldIndex < 0 || oldIndex >= entries.length) {
		return entries;
	}
	const list = [...entries];
	const [entry] = list.splice(oldIndex, 1);
	list.splice(clamp(newIndex, 0, list.length), 0, entry);
	return list;
}

export function shuffle(
	entries: PlaylistEntry[],
	pinnedIndex: number | null,
	rng: Rng
): PlaylistEntry[] {
	const order = entries.map((_, index) => index);
	let start = 0;
	if (pinnedIndex !== null) {
		[order[0], order[pinnedIndex]] = [order[pinnedIndex], order[0]];
		start = 1;
	}
	for (let i = order.length - 1; i > start; i--) {
		const j = start + Math.floor(rng() * (i - start + 1));
		[order[i], order[j]] = [order[j], order[i]];
	}
	return order.map((index) => entries[index]);
}

function clamp(value: number, low: number, high: number): number {
	return Math.min(Math.max(value, low), high);
}
