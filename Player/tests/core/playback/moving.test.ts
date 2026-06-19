import { describe, expect, it } from 'vitest';
import * as playbackTimeline from '$shared/core/playbackTimeline';
import * as playlists from '$shared/core/playlists';
import { IDLE_PLAYBACK, type PlaybackState } from '$shared/core';
import { entry } from '../fixtures';

const daySongs = (...ids: string[]) => ids.map((id) => entry(id, 10));

const playingAt = (cursor: number): PlaybackState => ({
	...IDLE_PLAYBACK,
	day: { cursor, gain: 1.0, resumeOffset: 0 }
});

describe('reindexAfterMove', () => {
	it('keeps the current entry selection while reordering', () => {
		const entries = daySongs('a', 'b', 'c');

		const moved = playlists.move(entries, 1, 0);
		const reindexed = playbackTimeline.reindexAfterMove(playingAt(0), 'Day', 1, 0, entries.length);

		expect(reindexed.day.cursor).toBe(1);
		expect(moved[reindexed.day.cursor!].id).toBe('a');
	});

	it('tracks the current entry when it is the one moved', () => {
		const entries = daySongs('a', 'b', 'c');

		const moved = playlists.move(entries, 0, 2);
		const reindexed = playbackTimeline.reindexAfterMove(playingAt(0), 'Day', 0, 2, entries.length);

		expect(reindexed.day.cursor).toBe(2);
		expect(moved[reindexed.day.cursor!].id).toBe('a');
	});

	it('ignores an out-of-range source index', () => {
		const entries = daySongs('a', 'b');

		const reindexed = playbackTimeline.reindexAfterMove(playingAt(0), 'Day', 5, 0, entries.length);

		expect(reindexed.day.cursor).toBe(0);
	});
});
