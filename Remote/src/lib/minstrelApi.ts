import type { Readable } from 'svelte/store';
import type { Phase, PlaybackState, SongDto } from './state';

// The reactive Playback ⨝ Playlist view the control surface renders, plus a liveness
// flag. Mobile's in-process implementation reports `connected: true` always.
export interface Snapshot {
	state: PlaybackState;
	connected: boolean;
}

// The single command/query interface the shared control-surface UI programs against
// (ADR-0008). Two implementations exist: RestApi (POST-to-Backend) and, later, Mobile's
// in-process Core. It carries no transport concern — HTTP status, SSE, EventSource stay
// behind the implementations.
export interface MinstrelApi {
	readonly snapshot: Readable<Snapshot>;

	fetchLibrary(): Promise<SongDto[]>;

	play(): Promise<void>;
	pause(): Promise<void>;
	switchPhase(): Promise<void>;
	selectSong(phase: Phase, index: number): Promise<void>;
	addSong(phase: Phase, songId: string): Promise<void>;
	removeSong(phase: Phase, index: number): Promise<void>;
	moveSong(phase: Phase, oldIndex: number, newIndex: number): Promise<void>;
	shuffle(phase: Phase): Promise<void>;
	setGain(phase: Phase, value: number): Promise<void>;
	startTimer(duration: number): Promise<void>;
	stopTimer(): Promise<void>;
}
