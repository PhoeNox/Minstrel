import { describe, expect, it } from 'vitest';
import * as playlists from '$shared/core/playlists';
import { entry } from '../fixtures';

const songs = (...ids: string[]) => ids.map((id) => entry(id, 10));

describe('indexOfSong', () => {
	it('returns the index of the matching song', () => {
		const entries = songs('a', 'b', 'c');

		expect(playlists.indexOfSong(entries, 'b')).toBe(1);
	});

	it('returns null when no entry matches', () => {
		const entries = songs('a', 'b');

		expect(playlists.indexOfSong(entries, 'z')).toBeNull();
	});

	it('returns the first index when the id appears more than once', () => {
		const entries = songs('a', 'b', 'b');

		expect(playlists.indexOfSong(entries, 'b')).toBe(1);
	});
});
