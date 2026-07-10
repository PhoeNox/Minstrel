import { writable, type Readable } from 'svelte/store';
import {
	countdownTimer,
	derivePosition,
	IDLE_PLAYBACK,
	IDLE_TIMER,
	playbackTimeline,
	playlists,
	type GamePhase,
	type PlaybackState as CoreState,
	type PlaylistEntry,
	type Rng,
	type TimerAnchor,
	type Track
} from '$shared/core';
import type { MinstrelApi, Snapshot } from '$shared/ui/minstrelApi';
import { emptyState, type PlaybackState, type PlaylistDto, type SongDto } from '$shared/ui/state';
import type { EngineTrack, PlaybackEngine } from './mobileAudioEngine';
import type { PersistedPlaylist, PersistedSession, SongRecord } from './musicStore';

// The slice of the MusicStore LocalSession coordinates: the Library to resolve songs
// from, and the persisted session it restores on start and writes back on every edit.
export interface SessionStore {
	songs(): Promise<SongRecord[]>;
	loadSession(): Promise<PersistedSession | null>;
	saveSession(session: PersistedSession): Promise<void>;
}

// The Gong gain — the Mobile analog of the Backend's configurable `GongOptions.Gain`,
// pitched to carry over the music without startling the table.
const GONG_GAIN = 3;

// LocalSession adds three hooks over MinstrelApi: `load` hydrates the in-memory state from
// the persisted session before the surface binds; `timer` is the countdown store the surface
// renders (the in-process stand-in for the Backend's `/timer/sse`); `tick` is the derive
// interval's expiry poll, the analog of `TimerSession.Tick` that retires an expired anchor.
export type LocalSession = MinstrelApi & {
	load(): Promise<void>;
	tick(): void;
	readonly timer: Readable<TimerAnchor | null>;
};

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
	rng: Rng = Math.random,
	engine?: PlaybackEngine
): LocalSession {
	let playback: CoreState = IDLE_PLAYBACK;
	let book: Book = { day: [], night: [] };
	let countdown: TimerAnchor = IDLE_TIMER;
	const state = writable<Snapshot>({ state: emptyState, connected: true });
	const timer = writable<TimerAnchor | null>(null);

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

	function engineTrackAt(phase: GamePhase, index: number | null): EngineTrack | null {
		const list = entries(phase);
		if (index === null || index < 0 || index >= list.length) {
			return null;
		}
		const entry = list[index];
		return { id: entry.id, title: entry.title, artist: entry.artist, length: entry.length };
	}

	// Drives the engine from the playback state a command just produced — the in-process
	// stand-in for Player's reconciler (dropped per ADR-0008, since there is no remote
	// snapshot to converge towards). Nothing Cued stops; a paused song fades out; an
	// unchanged lead only re-ramps its gain; a new lead crossfades in. `force` replays the
	// active current even when it is the same song id (a single-entry loop), where the
	// re-gain shortcut would otherwise leave the ended song silent.
	function renderAudio(force = false): void {
		if (engine === undefined) {
			return;
		}
		const position = playback.position;
		if (position.songId === null) {
			engine.stop();
			return;
		}
		if (!position.isPlaying) {
			engine.pause();
			return;
		}
		const gain = playbackTimeline.phaseOf(playback, playback.activePhase).gain;
		if (!force && engine.currentSongId === position.songId) {
			engine.setGain(gain);
			return;
		}
		const track = engineTrackAt(playback.activePhase, playbackTimeline.activeCursor(playback));
		if (track !== null) {
			engine.play(track, derivePosition(position, now()), gain);
		}
	}

	// Auto-advance: the engine signalled the lead song is a fade-length from its end (or the
	// lock-screen "next" was pressed), so step the active Playlist to the next entry — wrapping
	// last→first for a long game — and crossfade it in over the outgoing song's tail.
	function advanceCurrent(): void {
		playback = playbackTimeline.advance(playback, toTracks(entries(playback.activePhase)), now());
		publish();
		renderAudio(true);
		void persist();
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

	// The countdown surfaces as a nullable anchor, matching the Backend's timer wire shape
	// (a running anchor, or null when idle) so the shared countdown derivations read alike.
	function publishTimer(): void {
		timer.set(countdown.running ? countdown : null);
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

	const api: LocalSession = {
		snapshot: { subscribe: state.subscribe } as Readable<Snapshot>,
		timer: { subscribe: timer.subscribe } as Readable<TimerAnchor | null>,

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
			renderAudio();
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
			renderAudio();
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
			renderAudio();
			await persist();
		},

		async shuffle(phase) {
			const pinned = playbackTimeline.cursorOfStartedSong(playback, phase);
			const shuffled = playlists.shuffle(entries(phase), pinned, rng);
			replace(phase, shuffled);
			playback = playbackTimeline.reindexAfterShuffle(playback, phase);
			publish();
			renderAudio();
			await persist();
		},

		async selectSong(phase, index) {
			const list = entries(phase);
			if (index < 0 || index >= list.length) {
				return;
			}
			playback = playbackTimeline.select(playback, phase, index, trackAt(phase, index), now());
			publish();
			renderAudio();
			await persist();
		},

		async setGain(phase, value) {
			playback = playbackTimeline.setGain(playback, phase, value);
			publish();
			renderAudio();
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
			renderAudio();
		},

		async pause() {
			playback = playbackTimeline.pause(playback, now());
			publish();
			renderAudio();
		},

		async switchPhase() {
			const target: GamePhase = playback.activePhase === 'Day' ? 'Night' : 'Day';
			const activeCurrent = trackAt(playback.activePhase, playbackTimeline.activeCursor(playback));
			const targetCurrent = trackAt(target, playbackTimeline.cursorOf(playback, target));
			playback = playbackTimeline.switchPhase(playback, activeCurrent, targetCurrent, now());
			publish();
			renderAudio();
			await persist();
		},

		// The countdown runs concurrently with playback off the same coordinator: start arms
		// the anchor and the Gong (pre-scheduled on the audio clock so a backgrounded expiry
		// still sounds); stop retires both. No persistence — a countdown does not outlive a
		// restart, matching the Backend's in-memory TimerSession.
		async startTimer(duration) {
			countdown = countdownTimer.start(duration, now());
			publishTimer();
			engine?.scheduleGong(duration, GONG_GAIN);
		},

		async stopTimer() {
			countdown = IDLE_TIMER;
			publishTimer();
			engine?.cancelGong();
		},

		// The derive interval's expiry poll: once the anchor has run out, retire it so the
		// surface returns to idle. The Gong is not fired here — it was armed at start — so a
		// throttled background interval delays only the visual reset, never the cue.
		tick() {
			if (countdown.running && countdownTimer.hasExpired(countdown, now())) {
				countdown = IDLE_TIMER;
				publishTimer();
			}
		}
	};

	// The engine's own events fold back into the coordinator: a song ending or the
	// lock-screen "next" auto-advances, and the lock-screen play/pause mirror the surface.
	if (engine !== undefined) {
		engine.onEnded = advanceCurrent;
		engine.onMediaNext = advanceCurrent;
		engine.onMediaPlay = () => void api.play();
		engine.onMediaPause = () => void api.pause();
	}

	return api;
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
