import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { writable } from 'svelte/store';
import { PlaybackSync, SYNC_INTERVAL_MS, type SyncEngine } from '../src/lib/playbackSync';
import type { AudioOperation } from '../src/lib/reconciler';
import type { Connection } from '../src/lib/sseStore';
import { emptyState, type PlaybackState } from '../src/lib/state';

const NOW = 4000;

// A playing state with no playlist songs, so reconcile yields a single
// play/stop op without prefetch or auto-advance noise.
const playing = (songId: string): PlaybackState => ({
	...emptyState,
	isPlaying: true,
	currentSongId: songId,
	position: { songId, offset: 0, anchorTimestamp: NOW, isPlaying: true }
});

const connection = (state: PlaybackState, connected = true): Connection => ({ state, connected });

class FakeEngine implements SyncEngine {
	leadSongId: string | null = null;
	leadPosition: number | null = null;
	leadGain: number | null = null;
	loadedSongIds: string[] = [];
	readonly applied: AudioOperation[][] = [];
	rejection: unknown = null;
	private gate: (() => void) | null = null;

	apply(operations: AudioOperation[]): Promise<void> {
		this.applied.push(operations);
		if (this.rejection !== null) {
			return Promise.reject(this.rejection);
		}
		if (this.gate === null) {
			return Promise.resolve();
		}
		return new Promise((resolve) => {
			this.gate = resolve;
		});
	}

	stall(): void {
		this.gate = () => {};
	}

	release(): void {
		const resolve = this.gate;
		this.gate = null;
		resolve?.();
	}
}

const noopHooks = { onConnection: () => {}, onTick: () => {}, onError: () => {} };

const flush = () => Promise.resolve();

describe('PlaybackSync', () => {
	beforeEach(() => {
		vi.useFakeTimers();
		vi.setSystemTime(NOW);
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it('reconciles the current snapshot onto an engine the moment it attaches', async () => {
		const store = writable<Connection>(connection(playing('a')));
		const sync = new PlaybackSync(store, noopHooks);
		sync.start();
		const engine = new FakeEngine();

		sync.attach(engine);
		await flush();

		expect(engine.applied).toEqual([[{ type: 'play', songId: 'a', offset: 0, gain: 1 }]]);
		sync.stop();
	});

	it('re-syncs the engine when the playback store emits a new snapshot', async () => {
		const store = writable<Connection>(connection(playing('a')));
		const sync = new PlaybackSync(store, noopHooks);
		sync.start();
		const engine = new FakeEngine();
		sync.attach(engine);
		await flush();
		engine.leadSongId = 'a';

		store.set(connection(emptyState, true));
		await flush();

		expect(engine.applied.at(-1)).toEqual([{ type: 'stop' }]);
		sync.stop();
	});

	it('re-syncs on every heartbeat tick', async () => {
		const store = writable<Connection>(connection(playing('a')));
		const sync = new PlaybackSync(store, noopHooks);
		sync.start();
		const engine = new FakeEngine();
		sync.attach(engine);
		await flush();
		const before = engine.applied.length;

		await vi.advanceTimersByTimeAsync(SYNC_INTERVAL_MS);

		expect(engine.applied.length).toBe(before + 1);
		sync.stop();
	});

	it('buffers snapshots while no engine is attached and applies the latest on attach', async () => {
		const store = writable<Connection>(connection(emptyState, false));
		const sync = new PlaybackSync(store, noopHooks);
		sync.start();

		store.set(connection(playing('a')));
		await flush();
		const engine = new FakeEngine();
		sync.attach(engine);
		await flush();

		expect(engine.applied).toEqual([[{ type: 'play', songId: 'a', offset: 0, gain: 1 }]]);
		sync.stop();
	});

	it('drops overlapping triggers while a sync is in flight, then reconciles the latest snapshot', async () => {
		const store = writable<Connection>(connection(playing('a')));
		const sync = new PlaybackSync(store, noopHooks);
		sync.start();
		const engine = new FakeEngine();
		engine.stall();

		sync.attach(engine);
		await flush();
		store.set(connection(playing('b')));
		await vi.advanceTimersByTimeAsync(SYNC_INTERVAL_MS);
		expect(engine.applied.length).toBe(1);

		engine.release();
		await flush();
		await vi.advanceTimersByTimeAsync(SYNC_INTERVAL_MS);

		expect(engine.applied.length).toBe(2);
		expect(engine.applied[1]).toEqual([expect.objectContaining({ type: 'play', songId: 'b' })]);
		sync.stop();
	});

	it('surfaces connection state and heartbeat ticks to the hooks', async () => {
		const store = writable<Connection>(connection(emptyState, false));
		const onConnection = vi.fn();
		const onTick = vi.fn();
		const sync = new PlaybackSync(store, { onConnection, onTick, onError: () => {} });

		sync.start();
		store.set(connection(playing('a'), true));
		await vi.advanceTimersByTimeAsync(SYNC_INTERVAL_MS);

		expect(onConnection).toHaveBeenCalledWith(connection(emptyState, false));
		expect(onConnection).toHaveBeenCalledWith(connection(playing('a'), true));
		expect(onTick).toHaveBeenCalledTimes(1);
		sync.stop();
	});

	it('catches a rejecting apply and routes it to onError instead of throwing', async () => {
		const store = writable<Connection>(connection(playing('a')));
		const onError = vi.fn();
		const sync = new PlaybackSync(store, { ...noopHooks, onError });
		const engine = new FakeEngine();
		engine.rejection = new Error('decode failed');

		sync.start();
		sync.attach(engine);
		await flush();

		expect(onError).toHaveBeenCalledWith(engine.rejection);
		sync.stop();
	});

	it('clears the surfaced error on the next successful sync', async () => {
		const store = writable<Connection>(connection(playing('a')));
		const onError = vi.fn();
		const sync = new PlaybackSync(store, { ...noopHooks, onError });
		const engine = new FakeEngine();
		engine.rejection = new Error('decode failed');

		sync.start();
		sync.attach(engine);
		await flush();
		engine.rejection = null;

		await vi.advanceTimersByTimeAsync(SYNC_INTERVAL_MS);

		expect(onError).toHaveBeenLastCalledWith(null);
		sync.stop();
	});

	it('stops reconciling once stopped', async () => {
		const store = writable<Connection>(connection(playing('a')));
		const sync = new PlaybackSync(store, noopHooks);
		sync.start();
		const engine = new FakeEngine();
		sync.attach(engine);
		await flush();
		const after = engine.applied.length;

		sync.stop();
		store.set(connection(emptyState, true));
		await vi.advanceTimersByTimeAsync(SYNC_INTERVAL_MS * 4);
		await flush();

		expect(engine.applied.length).toBe(after);
	});
});
