import { describe, expect, it } from 'vitest';
import * as playlists from '$shared/core/playlists';
import { entry, seededRng } from '../fixtures';

const songs = (...ids: string[]) => ids.map((id) => entry(id, 100));

describe('shuffle', () => {
	it('keeps every entry', () => {
		const entries = songs('a', 'b', 'c', 'd', 'e');

		const { entries: shuffled } = playlists.shuffle(entries, seededRng(1));

		expect(shuffled.map((e) => e.id).sort()).toEqual(['a', 'b', 'c', 'd', 'e']);
	});

	it('is deterministic for a seeded random', () => {
		const entries = songs('a', 'b', 'c', 'd', 'e');

		const first = playlists.shuffle(entries, seededRng(42));
		const second = playlists.shuffle(entries, seededRng(42));

		expect(first.entries.map((e) => e.id)).toEqual(second.entries.map((e) => e.id));
		expect(first.permutation).toEqual(second.permutation);
	});

	it('returns a permutation that reproduces the shuffled order', () => {
		const entries = songs('a', 'b', 'c', 'd', 'e');

		const { entries: shuffled, permutation } = playlists.shuffle(entries, seededRng(7));

		expect(permutation.map((index) => entries[index].id)).toEqual(shuffled.map((e) => e.id));
	});
});
