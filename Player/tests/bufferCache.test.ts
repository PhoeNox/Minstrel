import { describe, expect, it } from 'vitest';
import { evictions, type CachedBuffer } from '../src/lib/bufferCache';

const entry = (songId: string, bytes = 10): CachedBuffer => ({ songId, bytes });

describe('evictions', () => {
	it('keeps everything while the total fits the limit', () => {
		const order = [entry('a'), entry('b'), entry('c')];

		expect(evictions(order, [], 100)).toEqual([]);
	});

	it('drops the oldest entries first once the limit is exceeded', () => {
		const order = [entry('a'), entry('b'), entry('c'), entry('d')];

		expect(evictions(order, [], 25)).toEqual(['a', 'b']);
	});

	it('never evicts the retained lead and prefetched next', () => {
		const order = [entry('lead'), entry('a'), entry('b'), entry('next')];

		expect(evictions(order, ['lead', 'next'], 20)).toEqual(['a', 'b']);
	});

	it('skips retained entries and keeps evicting older evictable ones to fit', () => {
		const order = [entry('a'), entry('lead'), entry('b'), entry('c')];

		expect(evictions(order, ['lead'], 20)).toEqual(['a', 'b']);
	});

	it('stops evicting as soon as the total fits, leaving newer entries cached', () => {
		const order = [entry('a'), entry('b'), entry('c'), entry('d')];

		expect(evictions(order, [], 35)).toEqual(['a']);
	});

	it('evicts every evictable entry when the retained set alone overflows', () => {
		const order = [entry('a'), entry('lead'), entry('next')];

		expect(evictions(order, ['lead', 'next'], 5)).toEqual(['a']);
	});
});
