import { describe, expect, it } from 'vitest';
import * as playlists from '$shared/core/playlists';
import { entry } from '../fixtures';

describe('add', () => {
	it('appends the entry to the end of the playlist', () => {
		const entries = [entry('a', 10), entry('b', 10)];

		const added = playlists.add(entries, entry('c', 20));

		expect(added.map((e) => e.id)).toEqual(['a', 'b', 'c']);
		expect(added[added.length - 1].length).toBe(20);
	});

	it('allows adding the same entry twice', () => {
		const entries = [entry('a', 10)];

		const added = playlists.add(entries, entry('a', 10));

		expect(added.map((e) => e.id)).toEqual(['a', 'a']);
	});
});
