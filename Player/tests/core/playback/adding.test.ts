import { describe, expect, it } from 'vitest';
import * as playbackTimeline from '$shared/core/playbackTimeline';
import { IDLE_PLAYBACK } from '$shared/core';

describe('reindexAfterAdd', () => {
	it('leaves the current entry and position untouched', () => {
		const state = playbackTimeline.play(
			{ ...IDLE_PLAYBACK, day: { cursor: 0, gain: 1.0, resumeOffset: 0 } },
			'a',
			1000
		);

		const reindexed = playbackTimeline.reindexAfterAdd(state, 'Day');

		expect(reindexed.day.cursor).toBe(0);
		expect(reindexed.position.songId).toBe('a');
		expect(reindexed.position).toEqual(state.position);
	});

	it('selects the first entry when adding to an empty playlist', () => {
		const reindexed = playbackTimeline.reindexAfterAdd(IDLE_PLAYBACK, 'Day');

		expect(reindexed.day.cursor).toBe(0);
	});
});
