import { describe, it, expect } from 'vitest';
import { findEvicted, partitionNew } from '../src/lib/library';
import type { SongRecord } from '../src/lib/musicStore';

const record = (id: string): SongRecord => ({
	id,
	title: `Title ${id}`,
	artist: `Artist ${id}`,
	length: 100,
	name: `${id}.mp3`,
	importedAt: 0
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
