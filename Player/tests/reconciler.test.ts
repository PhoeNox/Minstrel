import { describe, expect, it } from 'vitest';
import { reconcile } from '../src/lib/reconciler';
import { emptyState } from '../src/lib/state';

describe('reconcile', () => {
	it('emits a start operation for a desired song against an empty graph', () => {
		const operations = reconcile(
			{ ...emptyState, isPlaying: true, currentSongId: 'X' },
			{ playingSongId: null }
		);

		expect(operations).toEqual([{ type: 'start', songId: 'X' }]);
	});

	it('does nothing when the graph already plays the desired song', () => {
		const operations = reconcile(
			{ ...emptyState, isPlaying: true, currentSongId: 'X' },
			{ playingSongId: 'X' }
		);

		expect(operations).toEqual([]);
	});

	it('stops the current song when playback is paused', () => {
		const operations = reconcile(
			{ ...emptyState, isPlaying: false, currentSongId: 'X' },
			{ playingSongId: 'X' }
		);

		expect(operations).toEqual([{ type: 'stop', songId: 'X' }]);
	});

	it('stops the old song and starts the new one when the current song changes', () => {
		const operations = reconcile(
			{ ...emptyState, isPlaying: true, currentSongId: 'Y' },
			{ playingSongId: 'X' }
		);

		expect(operations).toEqual([
			{ type: 'stop', songId: 'X' },
			{ type: 'start', songId: 'Y' }
		]);
	});
});
