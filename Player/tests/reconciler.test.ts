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

const emptyGraph: AudioGraph = { playingSongId: null, loadedSongIds: [] };

const playingDay = (currentSongId: string, songs: SongDto[]): PlaybackState => ({
	...emptyState,
	isPlaying: true,
	currentSongId,
	position: { songId: currentSongId, offset: 0, anchorTimestamp: 4000, isPlaying: true },
	playlists: { ...emptyState.playlists, day: { songs, currentSongId } }
});

describe('reconcile', () => {
	it('starts the desired song at the derived offset against an empty graph', () => {
		const operations = reconcile(
			{ ...emptyState, isPlaying: true, currentSongId: 'X', position: at(10, 1000) },
			emptyGraph,
			4000
		);

		expect(operations).toEqual([{ type: 'start', songId: 'X', offset: 13 }]);
	});

	it('does nothing when the graph already plays the desired song', () => {
		const operations = reconcile(
			{ ...emptyState, isPlaying: true, currentSongId: 'X', position: at(0, 1000) },
			{ playingSongId: 'X', loadedSongIds: ['X'] },
			4000
		);

		expect(operations).toEqual([]);
	});

	it('stops the current song when playback is paused', () => {
		const operations = reconcile(
			{ ...emptyState, isPlaying: false, currentSongId: 'X', position: at(0, 1000, false) },
			{ playingSongId: 'X', loadedSongIds: ['X'] },
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
			{ playingSongId: 'X', loadedSongIds: ['X'] },
			4000
		);

		expect(operations).toEqual([
			{ type: 'stop', songId: 'X' },
			{ type: 'start', songId: 'Y', offset: 0 }
		]);
	});

	it('prefetches the next song in the active playlist', () => {
		const operations = reconcile(
			playingDay('a', [song('a'), song('b')]),
			{ playingSongId: 'a', loadedSongIds: ['a'] },
			4000
		);

		expect(operations).toEqual([{ type: 'prefetch', songId: 'b' }]);
	});

	it('wraps the prefetch to the first song at the end of the playlist', () => {
		const operations = reconcile(
			playingDay('b', [song('a'), song('b')]),
			{ playingSongId: 'b', loadedSongIds: ['b'] },
			4000
		);

		expect(operations).toEqual([{ type: 'prefetch', songId: 'a' }]);
	});

	it('does not prefetch a song that is already decoded', () => {
		const operations = reconcile(
			playingDay('a', [song('a'), song('b')]),
			{ playingSongId: 'a', loadedSongIds: ['a', 'b'] },
			4000
		);

		expect(operations).toEqual([]);
	});
});
