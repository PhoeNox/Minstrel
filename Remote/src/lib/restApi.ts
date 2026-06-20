import type { Readable } from 'svelte/store';
import * as rest from './commandClient';
import { fetchLibrary } from './libraryClient';
import { playbackStore } from './sseStore';
import type { MinstrelApi, Snapshot } from '$shared/ui/minstrelApi';
import type { Phase, SongDto } from '$shared/ui/state';

// The REST implementation of MinstrelApi: the Remote's original transport, now reached
// through the interface. SSE feeds the snapshot, commands POST to the Backend, and the
// Library is fetched over HTTP. The transport itself lives in commandClient, libraryClient,
// and sseStore; this adapter only wires them to the interface.
export class RestApi implements MinstrelApi {
	readonly snapshot: Readable<Snapshot> = playbackStore();

	fetchLibrary(): Promise<SongDto[]> {
		return fetchLibrary();
	}

	play(): Promise<void> {
		return rest.play();
	}

	pause(): Promise<void> {
		return rest.pause();
	}

	switchPhase(): Promise<void> {
		return rest.switchPhase();
	}

	selectSong(phase: Phase, index: number): Promise<void> {
		return rest.selectSong(phase, index);
	}

	addSong(phase: Phase, songId: string): Promise<void> {
		return rest.addSong(phase, songId);
	}

	removeSong(phase: Phase, index: number): Promise<void> {
		return rest.removeSong(phase, index);
	}

	moveSong(phase: Phase, oldIndex: number, newIndex: number): Promise<void> {
		return rest.moveSong(phase, oldIndex, newIndex);
	}

	shuffle(phase: Phase): Promise<void> {
		return rest.shuffle(phase);
	}

	setGain(phase: Phase, value: number): Promise<void> {
		return rest.setGain(phase, value);
	}

	startTimer(duration: number): Promise<void> {
		return rest.startTimer(duration);
	}

	stopTimer(): Promise<void> {
		return rest.stopTimer();
	}
}
