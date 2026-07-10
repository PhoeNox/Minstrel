import { describe, expect, it } from 'vitest';
import * as playlists from '$shared/core/playlists';
import { entry, seededRng } from '../fixtures';

const songs = (...ids: string[]) => ids.map((id) => entry(id, 100));

describe('shuffle', () => {
	it('keeps every entry', () => {
		const entries = songs('a', 'b', 'c', 'd', 'e');

		const shuffled = playlists.shuffle(entries, null, seededRng(1));

		expect(shuffled.map((e) => e.id).sort()).toEqual(['a', 'b', 'c', 'd', 'e']);
	});

	it('is deterministic for a seeded random', () => {
		const entries = songs('a', 'b', 'c', 'd', 'e');

		const first = playlists.shuffle(entries, null, seededRng(42));
		const second = playlists.shuffle(entries, null, seededRng(42));

		expect(first.map((e) => e.id)).toEqual(second.map((e) => e.id));
	});

	it('places the pinned entry first', () => {
		const entries = songs('a', 'b', 'c', 'd', 'e');

		const shuffled = playlists.shuffle(entries, 2, seededRng(1));

		expect(shuffled[0].id).toBe('c');
	});

	it('keeps every entry when pinning', () => {
		const entries = songs('a', 'b', 'c', 'd', 'e');

		const shuffled = playlists.shuffle(entries, 4, seededRng(3));

		expect(shuffled.map((e) => e.id).sort()).toEqual(['a', 'b', 'c', 'd', 'e']);
	});
});
