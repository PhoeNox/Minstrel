import type { Readable } from 'svelte/store';
import { reconcile, type AudioGraph, type AudioOperation } from './reconciler';
import type { Connection } from './sseStore';
import { emptyState, type PlaybackState } from './state';

export const SYNC_INTERVAL_MS = 250;

export interface SyncEngine extends AudioGraph {
	apply(operations: AudioOperation[]): Promise<void>;
}

export interface SyncHooks {
	onConnection: (connection: Connection) => void;
	onTick: () => void;
}

/**
 * Drives the live AudioEngine towards the Backend's playback snapshot: every
 * store emission and every heartbeat reconciles the desired state against the
 * engine's current graph and applies the resulting operations.
 */
export class PlaybackSync {
	private engine: SyncEngine | null = null;
	private snapshot: PlaybackState = emptyState;
	private syncing = false;
	private unsubscribe: (() => void) | null = null;
	private ticker: ReturnType<typeof setInterval> | null = null;

	constructor(
		private readonly playback: Readable<Connection>,
		private readonly hooks: SyncHooks
	) {}

	start(): void {
		this.unsubscribe = this.playback.subscribe((connection) => {
			this.snapshot = connection.state;
			this.hooks.onConnection(connection);
			void this.sync();
		});
		this.ticker = setInterval(() => {
			this.hooks.onTick();
			void this.sync();
		}, SYNC_INTERVAL_MS);
	}

	stop(): void {
		this.unsubscribe?.();
		this.unsubscribe = null;
		if (this.ticker !== null) {
			clearInterval(this.ticker);
			this.ticker = null;
		}
	}

	attach(engine: SyncEngine): void {
		this.engine = engine;
		void this.sync();
	}

	async sync(): Promise<void> {
		const engine = this.engine;
		if (!engine || this.syncing) {
			return;
		}

		this.syncing = true;
		try {
			await engine.apply(reconcile(this.snapshot, engine, Date.now()));
		} finally {
			this.syncing = false;
		}
	}
}
