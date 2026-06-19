import { describe, expect, it } from 'vitest';
import * as playlists from '$shared/core/playlists';
import { entry } from '../fixtures';

const songs = (...ids: string[]) => ids.map((id) => entry(id, 10));

describe('move', () => {
	it('reorders songs within the playlist', () => {
		const entries = songs('a', 'b', 'c');

		const moved = playlists.move(entries, 0, 2);

		expect(moved.map((e) => e.id)).toEqual(['b', 'c', 'a']);
	});

	it('ignores an out-of-range source index', () => {
		const entries = songs('a', 'b');

		const moved = playlists.move(entries, 5, 0);

		expect(moved.map((e) => e.id)).toEqual(['a', 'b']);
	});
});
