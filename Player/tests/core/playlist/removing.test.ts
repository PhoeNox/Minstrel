import { describe, expect, it } from 'vitest';
import * as playlists from '$shared/core/playlists';
import { entry } from '../fixtures';

const songs = (...ids: string[]) => ids.map((id) => entry(id, 10));

describe('removeAt', () => {
	it('drops the entry at the given index', () => {
		const entries = songs('a', 'b', 'c');

		const remaining = playlists.removeAt(entries, 1);

		expect(remaining.map((e) => e.id)).toEqual(['a', 'c']);
	});

	it('ignores an out-of-range index', () => {
		const entries = songs('a', 'b');

		const remaining = playlists.removeAt(entries, 5);

		expect(remaining.map((e) => e.id)).toEqual(['a', 'b']);
	});
});
