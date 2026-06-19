import { describe, it, expect } from 'vitest';
import { deriveSongId } from '../src/lib/songId';

const bytes = (values: number[]) => new Uint8Array(values).buffer;

describe('deriveSongId', () => {
	it('is a 16-character hex id', async () => {
		const id = await deriveSongId(bytes([1, 2, 3, 4]));

		expect(id).toMatch(/^[0-9a-f]{16}$/);
	});

	it('is stable for the same bytes across calls', async () => {
		const first = await deriveSongId(bytes([1, 2, 3, 4]));
		const second = await deriveSongId(bytes([1, 2, 3, 4]));

		expect(second).toBe(first);
	});

	it('differs for different bytes', async () => {
		const one = await deriveSongId(bytes([1, 2, 3, 4]));
		const other = await deriveSongId(bytes([4, 3, 2, 1]));

		expect(other).not.toBe(one);
	});
});
