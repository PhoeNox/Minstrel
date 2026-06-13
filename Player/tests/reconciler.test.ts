import { describe, expect, it } from 'vitest';
import { reconcile } from '../src/lib/reconciler';
import { emptyState } from '../src/lib/state';

const at = (offset: number, anchorTimestamp: number, isPlaying = true) => ({
	songId: 'X',
	offset,
	anchorTimestamp,
	isPlaying
});

describe('reconcile', () => {
	it('starts the desired song at the derived offset against an empty graph', () => {
		const operations = reconcile(
			{ ...emptyState, isPlaying: true, currentSongId: 'X', position: at(10, 1000) },
			{ playingSongId: null },
			4000
		);

		expect(operations).toEqual([{ type: 'start', songId: 'X', offset: 13 }]);
	});

	it('does nothing when the graph already plays the desired song', () => {
		const operations = reconcile(
			{ ...emptyState, isPlaying: true, currentSongId: 'X', position: at(0, 1000) },
			{ playingSongId: 'X' },
			4000
		);

		expect(operations).toEqual([]);
	});

	it('stops the current song when playback is paused', () => {
		const operations = reconcile(
			{ ...emptyState, isPlaying: false, currentSongId: 'X', position: at(0, 1000, false) },
			{ playingSongId: 'X' },
			4000
		);

		expect(operations).toEqual([{ type: 'stop', songId: 'X' }]);
	});

	it('stops the old song and starts the new one at its offset when the current song changes', () => {
		const operations = reconcile(
			{
				...emptyState,
				isPlaying: true,
				currentSongId: 'Y',
				position: { songId: 'Y', offset: 0, anchorTimestamp: 4000, isPlaying: true }
			},
			{ playingSongId: 'X' },
			4000
		);

		expect(operations).toEqual([
			{ type: 'stop', songId: 'X' },
			{ type: 'start', songId: 'Y', offset: 0 }
		]);
	});
});
