import { describe, expect, it } from 'vitest';
import { reconcile, type AudioGraph } from '../src/lib/reconciler';
import { emptyState, type PlaybackState, type SongDto } from '../src/lib/state';

const at = (offset: number, anchorTimestamp: number, isPlaying = true) => ({
	songId: 'X',
	offset,
	anchorTimestamp,
	isPlaying
});

const song = (id: string, length = 100): SongDto => ({ id, title: id, artist: id, length });

const emptyGraph: AudioGraph = {
	leadSongId: null,
	leadPosition: null,
	leadGain: null,
	loadedSongIds: [],
	retainedSongIds: []
};

const playingDay = (currentSongId: string, songs: SongDto[], gain = 1): PlaybackState => ({
	...emptyState,
	isPlaying: true,
	currentSongId,
	position: { songId: currentSongId, offset: 0, anchorTimestamp: 4000, isPlaying: true },
	playlists: {
		...emptyState.playlists,
		day: { songs, currentIndex: songs.findIndex((s) => s.id === currentSongId), gain }
	}
});

describe('reconcile', () => {
	it('fades the desired song in at the derived offset against an empty graph', () => {
		const operations = reconcile(
			{ ...emptyState, isPlaying: true, currentSongId: 'X', position: at(10, 1000) },
			emptyGraph,
			4000
		);

		expect(operations).toEqual([{ type: 'play', songId: 'X', offset: 13, gain: 1 }]);
	});

	it('does nothing when the lead already plays the desired song in step', () => {
		const operations = reconcile(
			playingDay('a', [song('a'), song('b')]),
			{ leadSongId: 'a', leadPosition: 0, leadGain: 1, loadedSongIds: ['a', 'b'], retainedSongIds: ['b'] },
			4000
		);

		expect(operations).toEqual([]);
	});

	it('stops the lead when playback is paused', () => {
		const operations = reconcile(
			{ ...emptyState, isPlaying: false, currentSongId: 'X', position: at(0, 1000, false) },
			{ leadSongId: 'X', leadPosition: 3, leadGain: 1, loadedSongIds: ['X'], retainedSongIds: [] },
			4000
		);

		expect(operations).toEqual([{ type: 'stop' }]);
	});

	it('crossfades to the backend song when the lead is an unrelated one', () => {
		const operations = reconcile(
			playingDay('a', [song('a'), song('b'), song('c')]),
			{ leadSongId: 'c', leadPosition: 50, leadGain: 1, loadedSongIds: ['a', 'b', 'c'], retainedSongIds: ['b'] },
			4000
		);

		expect(operations).toEqual([{ type: 'play', songId: 'a', offset: 0, gain: 1 }]);
	});

	it('crossfades into the next song as the current one nears its end', () => {
		const operations = reconcile(
			playingDay('a', [song('a'), song('b')]),
			{ leadSongId: 'a', leadPosition: 96, leadGain: 1, loadedSongIds: ['a', 'b'], retainedSongIds: ['b'] },
			4000
		);

		expect(operations).toEqual([{ type: 'play', songId: 'b', offset: 0, gain: 1 }]);
	});

	it('crossfades a single-song playlist into a fresh copy of itself when it loops', () => {
		const operations = reconcile(
			playingDay('a', [song('a')]),
			{ leadSongId: 'a', leadPosition: 96, leadGain: 1, loadedSongIds: ['a'], retainedSongIds: [] },
			4000
		);

		expect(operations).toEqual([{ type: 'play', songId: 'a', offset: 0, gain: 1 }]);
	});

	it('does not crossfade before the current song nears its end', () => {
		const operations = reconcile(
			playingDay('a', [song('a'), song('b')]),
			{ leadSongId: 'a', leadPosition: 50, leadGain: 1, loadedSongIds: ['a', 'b'], retainedSongIds: ['b'] },
			4000
		);

		expect(operations).toEqual([]);
	});

	it('does not anticipate songs shorter than the fade window', () => {
		const operations = reconcile(
			playingDay('a', [song('a', 3)]),
			{ leadSongId: 'a', leadPosition: 3, leadGain: 1, loadedSongIds: ['a'], retainedSongIds: [] },
			4000
		);

		expect(operations).toEqual([]);
	});

	it('holds steady while the lead has already crossfaded ahead of the backend', () => {
		const operations = reconcile(
			playingDay('a', [song('a'), song('b')]),
			{ leadSongId: 'b', leadPosition: 2, leadGain: 1, loadedSongIds: ['a', 'b'], retainedSongIds: ['b'] },
			4000
		);

		expect(operations).toEqual([]);
	});

	it('retains and prefetches the next song while the current one plays', () => {
		const operations = reconcile(
			playingDay('a', [song('a'), song('b')]),
			{ leadSongId: 'a', leadPosition: 10, leadGain: 1, loadedSongIds: ['a'], retainedSongIds: [] },
			4000
		);

		expect(operations).toEqual([
			{ type: 'retain', songIds: ['b'] },
			{ type: 'prefetch', songId: 'b' }
		]);
	});

	it('wraps the prefetch to the first song at the end of the playlist', () => {
		const operations = reconcile(
			playingDay('b', [song('a'), song('b')]),
			{ leadSongId: 'b', leadPosition: 10, leadGain: 1, loadedSongIds: ['b'], retainedSongIds: [] },
			4000
		);

		expect(operations).toEqual([
			{ type: 'retain', songIds: ['a'] },
			{ type: 'prefetch', songId: 'a' }
		]);
	});

	it('prefetches the inactive phase Current Entry so a switch can crossfade at once', () => {
		const state: PlaybackState = {
			...playingDay('a', [song('a'), song('b')]),
			playlists: {
				day: { songs: [song('a'), song('b')], currentIndex: 0, gain: 1 },
				night: { songs: [song('n1'), song('n2')], currentIndex: 1, gain: 1 }
			}
		};

		const operations = reconcile(
			state,
			{ leadSongId: 'a', leadPosition: 10, leadGain: 1, loadedSongIds: ['a', 'b'], retainedSongIds: [] },
			4000
		);

		expect(operations).toEqual([
			{ type: 'retain', songIds: ['b', 'n2'] },
			{ type: 'prefetch', songId: 'n2' }
		]);
	});

	it('prefetches both the next song and the inactive phase Current Entry', () => {
		const state: PlaybackState = {
			...playingDay('a', [song('a'), song('b')]),
			playlists: {
				day: { songs: [song('a'), song('b')], currentIndex: 0, gain: 1 },
				night: { songs: [song('n1'), song('n2')], currentIndex: 1, gain: 1 }
			}
		};

		const operations = reconcile(
			state,
			{ leadSongId: 'a', leadPosition: 10, leadGain: 1, loadedSongIds: ['a'], retainedSongIds: [] },
			4000
		);

		expect(operations).toEqual([
			{ type: 'retain', songIds: ['b', 'n2'] },
			{ type: 'prefetch', songId: 'b' },
			{ type: 'prefetch', songId: 'n2' }
		]);
	});

	it('re-retains a loaded warm target that fell out of retention, without refetching it', () => {
		const state: PlaybackState = {
			...playingDay('a', [song('a'), song('b')]),
			playlists: {
				day: { songs: [song('a'), song('b')], currentIndex: 0, gain: 1 },
				night: { songs: [song('n1'), song('n2')], currentIndex: 1, gain: 1 }
			}
		};

		const operations = reconcile(
			state,
			{ leadSongId: 'a', leadPosition: 10, leadGain: 1, loadedSongIds: ['a', 'b', 'n2'], retainedSongIds: ['b'] },
			4000
		);

		expect(operations).toEqual([{ type: 'retain', songIds: ['b', 'n2'] }]);
	});

	it('leaves retention untouched while it already matches the warm targets in any order', () => {
		const state: PlaybackState = {
			...playingDay('a', [song('a'), song('b')]),
			playlists: {
				day: { songs: [song('a'), song('b')], currentIndex: 0, gain: 1 },
				night: { songs: [song('n1'), song('n2')], currentIndex: 1, gain: 1 }
			}
		};

		const operations = reconcile(
			state,
			{ leadSongId: 'a', leadPosition: 10, leadGain: 1, loadedSongIds: ['a', 'b', 'n2'], retainedSongIds: ['n2', 'b'] },
			4000
		);

		expect(operations).toEqual([]);
	});

	it('sets the gain on the lead when the phase gain changes', () => {
		const operations = reconcile(
			playingDay('a', [song('a'), song('b')], 0.4),
			{ leadSongId: 'a', leadPosition: 10, leadGain: 1, loadedSongIds: ['a', 'b'], retainedSongIds: ['b'] },
			4000
		);

		expect(operations).toEqual([{ type: 'set-gain', gain: 0.4 }]);
	});

	it('crossfades the next song in at the phase gain as the current one ends', () => {
		const operations = reconcile(
			playingDay('a', [song('a'), song('b')], 0.4),
			{ leadSongId: 'a', leadPosition: 96, leadGain: 0.4, loadedSongIds: ['a', 'b'], retainedSongIds: ['b'] },
			4000
		);

		expect(operations).toEqual([{ type: 'play', songId: 'b', offset: 0, gain: 0.4 }]);
	});
});
