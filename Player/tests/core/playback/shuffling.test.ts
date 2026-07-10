import { describe, expect, it } from 'vitest';
import * as playbackTimeline from '$shared/core/playbackTimeline';
import { IDLE_PLAYBACK, type PlaybackState } from '$shared/core';

function activePlaying(cursor: number, songId: string): PlaybackState {
	return playbackTimeline.playAt(
		{ ...IDLE_PLAYBACK, day: { cursor, gain: 1.0, resumeOffset: 0 } },
		songId,
		30,
		1000
	);
}

describe('cursorOfStartedSong', () => {
	it('pins the active phase cursor when its song is playing', () => {
		const state = activePlaying(1, 'b');

		const pinned = playbackTimeline.cursorOfStartedSong(state, 'Day');

		expect(pinned).toBe(1);
	});

	it('pins the active phase cursor when its song is paused midway', () => {
		const state = playbackTimeline.pause(activePlaying(2, 'c'), 2000);

		const pinned = playbackTimeline.cursorOfStartedSong(state, 'Day');

		expect(pinned).toBe(2);
	});

	it('pins nothing when the active phase never started a song', () => {
		const state = { ...IDLE_PLAYBACK, day: { cursor: 1, gain: 1.0, resumeOffset: 0 } };

		const pinned = playbackTimeline.cursorOfStartedSong(state, 'Day');

		expect(pinned).toBeNull();
	});

	it('pins the inactive phase cursor when its song was started before switching away', () => {
		const state = { ...IDLE_PLAYBACK, night: { cursor: 2, gain: 1.0, resumeOffset: 35 } };

		const pinned = playbackTimeline.cursorOfStartedSong(state, 'Night');

		expect(pinned).toBe(2);
	});

	it('pins nothing when the inactive phase song never started', () => {
		const state = { ...IDLE_PLAYBACK, night: { cursor: 2, gain: 1.0, resumeOffset: 0 } };

		const pinned = playbackTimeline.cursorOfStartedSong(state, 'Night');

		expect(pinned).toBeNull();
	});
});

describe('reindexAfterShuffle', () => {
	it('moves the cursor to the front without disturbing playback', () => {
		const state = activePlaying(3, 'd');

		const reindexed = playbackTimeline.reindexAfterShuffle(state, 'Day');

		expect(reindexed.day.cursor).toBe(0);
		expect(reindexed.position).toEqual(state.position);
	});

	it('moves the inactive phase cursor to the front', () => {
		const state = {
			...activePlaying(0, 'a'),
			night: { cursor: 2, gain: 1.0, resumeOffset: 0 }
		};

		const reindexed = playbackTimeline.reindexAfterShuffle(state, 'Night');

		expect(reindexed.night.cursor).toBe(0);
		expect(reindexed.position).toEqual(state.position);
	});

	it('keeps a missing cursor missing', () => {
		const state = IDLE_PLAYBACK;

		const reindexed = playbackTimeline.reindexAfterShuffle(state, 'Day');

		expect(reindexed.day.cursor).toBeNull();
	});
});
