import { describe, expect, it } from 'vitest';
import * as playbackTimeline from '$shared/core/playbackTimeline';
import * as playlists from '$shared/core/playlists';
import { IDLE_PLAYBACK, type PlaybackState, type PlaylistEntry } from '$shared/core';
import { entry, seededRng } from '../fixtures';

function activePlaying(
	cursor: number,
	...ids: string[]
): { entries: PlaylistEntry[]; state: PlaybackState } {
	const entries = ids.map((id) => entry(id, 100));
	const state = playbackTimeline.playAt(
		{ ...IDLE_PLAYBACK, day: { cursor, gain: 1.0, resumeOffset: 0 } },
		ids[cursor],
		30,
		1000
	);
	return { entries, state };
}

describe('reindexAfterShuffle', () => {
	it('tracks the current entry to its new index without disturbing playback', () => {
		const { entries, state } = activePlaying(1, 'a', 'b', 'c', 'd', 'e');

		const { entries: shuffled, permutation } = playlists.shuffle(entries, seededRng(12345));
		const reindexed = playbackTimeline.reindexAfterShuffle(state, 'Day', permutation);

		expect(shuffled[reindexed.day.cursor!].id).toBe('b');
		expect(reindexed.position.songId).toBe('b');
		expect(reindexed.position).toEqual(state.position);
	});

	it('preserves the inactive playlist current entry', () => {
		const night = ['x', 'y', 'z', 'w'].map((id) => entry(id, 100));
		const state = playbackTimeline.playAt(
			{
				...IDLE_PLAYBACK,
				day: { cursor: 0, gain: 1.0, resumeOffset: 0 },
				night: { cursor: 2, gain: 1.0, resumeOffset: 0 }
			},
			'a',
			30,
			1000
		);

		const { entries: shuffled, permutation } = playlists.shuffle(night, seededRng(7));
		const reindexed = playbackTimeline.reindexAfterShuffle(state, 'Night', permutation);

		expect(shuffled[reindexed.night.cursor!].id).toBe('z');
		expect(reindexed.position).toEqual(state.position);
	});
});
