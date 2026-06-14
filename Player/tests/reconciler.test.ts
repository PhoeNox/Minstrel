import { describe, expect, it } from 'vitest';
import { reconcile, type AudioGraph } from '../src/lib/reconciler';
import { emptyState, type PlaybackState, type SongDto } from '../src/lib/state';

const at = (offset: number, anchorTimestamp: number, isPlaying = true) => ({
	songId: 'X',
	offset,
	anchorTimestamp,
	isPlaying
});

const song = (id: string): SongDto => ({ id, title: id, artist: id, length: 100 });

const emptyGraph: AudioGraph = { playingSongId: null, loadedSongIds: [], gain: 1 };

const playingDay = (currentSongId: string, songs: SongDto[], gain = 1): PlaybackState => ({
	...emptyState,
	isPlaying: true,
	currentSongId,
	position: { songId: currentSongId, offset: 0, anchorTimestamp: 4000, isPlaying: true },
	playlists: { ...emptyState.playlists, day: { songs, currentSongId, gain } }
});

describe('reconcile', () => {
	it('fades in the desired song at the derived offset against an empty graph', () => {
		const operations = reconcile(
			{ ...emptyState, isPlaying: true, currentSongId: 'X', position: at(10, 1000) },
			emptyGraph,
			4000
		);

		expect(operations).toEqual([{ type: 'fade-in', songId: 'X', offset: 13, gain: 1 }]);
	});

	it('does nothing when the graph already plays the desired song', () => {
		const operations = reconcile(
			{ ...emptyState, isPlaying: true, currentSongId: 'X', position: at(0, 1000) },
			{ playingSongId: 'X', loadedSongIds: ['X'], gain: 1 },
			4000
		);

		expect(operations).toEqual([]);
	});

	it('fades out the current song when playback is paused', () => {
		const operations = reconcile(
			{ ...emptyState, isPlaying: false, currentSongId: 'X', position: at(0, 1000, false) },
			{ playingSongId: 'X', loadedSongIds: ['X'], gain: 1 },
			4000
		);

		expect(operations).toEqual([{ type: 'fade-out', songId: 'X' }]);
	});

	it('crossfades the old song out and the new one in when the current song changes', () => {
		const operations = reconcile(
			{
				...emptyState,
				isPlaying: true,
				currentSongId: 'Y',
				position: { songId: 'Y', offset: 0, anchorTimestamp: 4000, isPlaying: true }
			},
			{ playingSongId: 'X', loadedSongIds: ['X'], gain: 1 },
			4000
		);

		expect(operations).toEqual([
			{ type: 'fade-out', songId: 'X' },
			{ type: 'fade-in', songId: 'Y', offset: 0, gain: 1 }
		]);
	});

	it('prefetches the next song in the active playlist', () => {
		const operations = reconcile(
			playingDay('a', [song('a'), song('b')]),
			{ playingSongId: 'a', loadedSongIds: ['a'], gain: 1 },
			4000
		);

		expect(operations).toEqual([{ type: 'prefetch', songId: 'b' }]);
	});

	it('wraps the prefetch to the first song at the end of the playlist', () => {
		const operations = reconcile(
			playingDay('b', [song('a'), song('b')]),
			{ playingSongId: 'b', loadedSongIds: ['b'], gain: 1 },
			4000
		);

		expect(operations).toEqual([{ type: 'prefetch', songId: 'a' }]);
	});

	it('does not prefetch a song that is already decoded', () => {
		const operations = reconcile(
			playingDay('a', [song('a'), song('b')]),
			{ playingSongId: 'a', loadedSongIds: ['a', 'b'], gain: 1 },
			4000
		);

		expect(operations).toEqual([]);
	});

	it('sets the gain on the sounding song when the phase gain changes', () => {
		const operations = reconcile(
			playingDay('a', [song('a'), song('b')], 0.4),
			{ playingSongId: 'a', loadedSongIds: ['a', 'b'], gain: 1 },
			4000
		);

		expect(operations).toEqual([{ type: 'set-gain', songId: 'a', gain: 0.4 }]);
	});

	it('fades the new song in at the phase gain when the song changes', () => {
		const operations = reconcile(
			playingDay('a', [song('a'), song('b')], 0.4),
			{ playingSongId: 'b', loadedSongIds: ['b'], gain: 0.4 },
			4000
		);

		expect(operations).toEqual([
			{ type: 'fade-out', songId: 'b' },
			{ type: 'fade-in', songId: 'a', offset: 0, gain: 0.4 }
		]);
	});
});
