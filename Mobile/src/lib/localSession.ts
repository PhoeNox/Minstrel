import { writable, type Readable } from 'svelte/store';
import {
	IDLE_PLAYBACK,
	playbackTimeline,
	playlists,
	type GamePhase,
	type PlaybackState as CoreState,
	type PlaylistEntry,
	type Rng,
	type Track
} from '$shared/core';
import type { MinstrelApi, Snapshot } from '$shared/ui/minstrelApi';
import { emptyState, type PlaybackState, type PlaylistDto, type SongDto } from '$shared/ui/state';
import type { PersistedPlaylist, PersistedSession, SongRecord } from './musicStore';

// The slice of the MusicStore LocalSession coordinates: the Library to resolve songs
// from, and the persisted session it restores on start and writes back on every edit.
export interface SessionStore {
	songs(): Promise<SongRecord[]>;
	loadSession(): Promise<PersistedSession | null>;
	saveSession(session: PersistedSession): Promise<void>;
}

// LocalSession adds one lifecycle hook over MinstrelApi: `load` hydrates the in-memory
// state from the persisted session before the surface binds to the snapshot.
export type LocalSession = MinstrelApi & { load(): Promise<void> };

interface Book {
	day: PlaylistEntry[];
	night: PlaylistEntry[];
}

// Mobile's in-process coordinator — the analog of the Backend's PlaybackSession minus the
// lock and SSE (the single-threaded JS event loop serializes mutation). Holds live
// PlaybackState + both Playlists; each command drives a pure `core` transform, reconciles
// the index cursor from the structural change, persists, and emits a new snapshot.
export function createLocalSession(
	store: SessionStore,
	now: () => number = () => Date.now(),
	rng: Rng = Math.random
): LocalSession {
	let playback: CoreState = IDLE_PLAYBACK;
	let book: Book = { day: [], night: [] };
	const state = writable<Snapshot>({ state: emptyState, connected: true });

	function entries(phase: GamePhase): PlaylistEntry[] {
		return phase === 'Day' ? book.day : book.night;
	}

	function replace(phase: GamePhase, list: PlaylistEntry[]): void {
		book = phase === 'Day' ? { ...book, day: list } : { ...book, night: list };
	}

	function trackAt(phase: GamePhase, index: number | null): Track | null {
		const list = entries(phase);
		return index !== null && index >= 0 && index < list.length
			? { id: list[index].id, length: list[index].length }
			: null;
	}

	async function persist(): Promise<void> {
		await store.saveSession({
			activePhase: playback.activePhase,
			day: persistedPlaylist('Day'),
			night: persistedPlaylist('Night')
		});
	}

	function persistedPlaylist(phase: GamePhase): PersistedPlaylist {
		const phaseState = playbackTimeline.phaseOf(playback, phase);
		return { entries: entries(phase), cursor: phaseState.cursor, gain: phaseState.gain };
	}

	function publish(): void {
		state.set({ state: toSnapshot(), connected: true });
	}

	function toSnapshot(): PlaybackState {
		return {
			phase: playback.activePhase,
			isPlaying: playback.position.isPlaying,
			currentSongId: playback.position.songId,
			playlists: { day: toPlaylistDto('Day'), night: toPlaylistDto('Night') },
			position: playback.position
		};
	}

	function toPlaylistDto(phase: GamePhase): PlaylistDto {
		const phaseState = playbackTimeline.phaseOf(playback, phase);
		return {
			songs: entries(phase).map(toSongDto),
			currentIndex: phaseState.cursor,
			gain: phaseState.gain,
			resumeOffset: phaseState.resumeOffset
		};
	}

	return {
		snapshot: { subscribe: state.subscribe } as Readable<Snapshot>,

		async load() {
			const persisted = await store.loadSession();
			if (persisted !== null) {
				book = { day: persisted.day.entries, night: persisted.night.entries };
				playback = {
					...IDLE_PLAYBACK,
					activePhase: persisted.activePhase,
					day: { cursor: persisted.day.cursor, gain: persisted.day.gain, resumeOffset: 0 },
					night: { cursor: persisted.night.cursor, gain: persisted.night.gain, resumeOffset: 0 }
				};
			}
			publish();
		},

		async fetchLibrary() {
			return (await store.songs()).map(toSongDto);
		},

		async addSong(phase, songId) {
			const song = (await store.songs()).find((record) => record.id === songId);
			if (song === undefined) {
				return;
			}
			replace(phase, playlists.add(entries(phase), toEntry(song)));
			playback = playbackTimeline.reindexAfterAdd(playback, phase);
			publish();
			await persist();
		},

		async removeSong(phase, index) {
			const list = entries(phase);
			if (index < 0 || index >= list.length) {
				return;
			}
			const removed = playlists.removeAt(list, index);
			replace(phase, removed);
			playback = playbackTimeline.reindexAfterRemove(
				playback,
				phase,
				index,
				list.length,
				toTracks(removed),
				now()
			);
			publish();
			await persist();
		},

		async moveSong(phase, oldIndex, newIndex) {
			const list = entries(phase);
			if (oldIndex < 0 || oldIndex >= list.length) {
				return;
			}
			replace(phase, playlists.move(list, oldIndex, newIndex));
			playback = playbackTimeline.reindexAfterMove(playback, phase, oldIndex, newIndex, list.length);
			publish();
			await persist();
		},

		async shuffle(phase) {
			const { entries: shuffled, permutation } = playlists.shuffle(entries(phase), rng);
			replace(phase, shuffled);
			playback = playbackTimeline.reindexAfterShuffle(playback, phase, permutation);
			publish();
			await persist();
		},

		async selectSong(phase, index) {
			const list = entries(phase);
			if (index < 0 || index >= list.length) {
				return;
			}
			playback = playbackTimeline.select(playback, phase, index, trackAt(phase, index), now());
			publish();
			await persist();
		},

		async setGain(phase, value) {
			playback = playbackTimeline.setGain(playback, phase, value);
			publish();
			await persist();
		},

		async play() {
			const index = playbackTimeline.activeCursor(playback);
			if (index === null) {
				return;
			}
			const phase = playback.activePhase;
			playback = playbackTimeline.select(playback, phase, index, trackAt(phase, index), now());
			publish();
		},

		async pause() {
			playback = playbackTimeline.pause(playback, now());
			publish();
		},

		async switchPhase() {
			const target: GamePhase = playback.activePhase === 'Day' ? 'Night' : 'Day';
			const activeCurrent = trackAt(playback.activePhase, playbackTimeline.activeCursor(playback));
			const targetCurrent = trackAt(target, playbackTimeline.cursorOf(playback, target));
			playback = playbackTimeline.switchPhase(playback, activeCurrent, targetCurrent, now());
			publish();
			await persist();
		},

		// The Timer is a separate concern built on this coordinator in a later slice; the
		// playlist-management surface never raises these.
		async startTimer() {},
		async stopTimer() {}
	};
}

function toEntry(record: SongRecord): PlaylistEntry {
	return {
		id: record.id,
		path: record.name,
		title: record.title,
		artist: record.artist,
		length: record.length
	};
}

function toTracks(entries: PlaylistEntry[]): Track[] {
	return entries.map((entry) => ({ id: entry.id, length: entry.length }));
}

function toSongDto(record: SongRecord | PlaylistEntry): SongDto {
	return { id: record.id, title: record.title, artist: record.artist, length: record.length };
}
