import { describe, it, expect } from 'vitest';
import type { PlaylistDto, PlaylistsDto } from '$shared/ui/state';
import { filterLibrary, findEvicted, partitionNew } from '../src/lib/library';
import type { SongRecord } from '../src/lib/musicStore';

const record = (id: string): SongRecord => ({
	id,
	title: `Title ${id}`,
	artist: `Artist ${id}`,
	length: 100,
	name: `${id}.mp3`,
	importedAt: 0
});

const playlist = (songs: SongRecord[]): PlaylistDto => ({
	songs,
	currentIndex: null,
	gain: 1,
	resumeOffset: 0
});

const playlists = (day: SongRecord[], night: SongRecord[] = []): PlaylistsDto => ({
	day: playlist(day),
	night: playlist(night)
});

describe('partitionNew', () => {
	it('keeps candidates absent from the library as additions', () => {
		const result = partitionNew(new Set(['a']), [record('b'), record('c')]);

		expect(result.added.map((song) => song.id)).toEqual(['b', 'c']);
		expect(result.duplicates).toHaveLength(0);
	});

	it('routes candidates already in the library to duplicates', () => {
		const result = partitionNew(new Set(['a', 'b']), [record('a'), record('c')]);

		expect(result.added.map((song) => song.id)).toEqual(['c']);
		expect(result.duplicates.map((song) => song.id)).toEqual(['a']);
	});

	it('dedups repeated candidates within the one batch', () => {
		const result = partitionNew(new Set(), [record('a'), record('a')]);

		expect(result.added.map((song) => song.id)).toEqual(['a']);
		expect(result.duplicates.map((song) => song.id)).toEqual(['a']);
	});
});

describe('findEvicted', () => {
	it('returns the records whose bytes are no longer stored', () => {
		const records = [record('a'), record('b'), record('c')];

		const evicted = findEvicted(records, new Set(['a', 'c']));

		expect(evicted.map((song) => song.id)).toEqual(['b']);
	});

	it('returns nothing when every record still has its bytes', () => {
		const records = [record('a'), record('b')];

		const evicted = findEvicted(records, new Set(['a', 'b']));

		expect(evicted).toHaveLength(0);
	});
});

describe('filterLibrary', () => {
	it('narrows to songs in neither playlist when the unused filter is on', () => {
		const songs = [record('a'), record('b'), record('c')];

		const result = filterLibrary(songs, playlists([songs[0]], [songs[2]]), '', true);

		expect(result.map((song) => song.id)).toEqual(['b']);
	});

	it('combines the unused filter with the search as AND', () => {
		const songs = [record('a'), record('b')];

		const result = filterLibrary(songs, playlists([]), 'Title a', true);

		expect(result.map((song) => song.id)).toEqual(['a']);
	});

	it('matches the search against title and artist across the whole library', () => {
		const songs = [record('a'), record('b')];

		const result = filterLibrary(songs, playlists([songs[0]]), 'Artist a', false);

		expect(result.map((song) => song.id)).toEqual(['a']);
	});

	it('keeps an evicted record that is in no playlist under the unused filter', () => {
		const songs = [record('a'), record('b')];
		const evicted = findEvicted(songs, new Set(['a']));

		const result = filterLibrary(songs, playlists([songs[0]]), '', true);

		expect(result).toEqual(evicted);
	});
});
