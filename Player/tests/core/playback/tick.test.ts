import { describe, expect, it } from 'vitest';
import * as playbackTimeline from '$shared/core/playbackTimeline';
import { IDLE_PLAYBACK } from '$shared/core';

describe('tick', () => {
	it('advances the anchor while playing', () => {
		const playing = playbackTimeline.play(IDLE_PLAYBACK, 'song-1', 1000);

		const ticked = playbackTimeline.tick(playing, [], 4000);

		expect(ticked.position.offset).toBe(3);
		expect(ticked.position.anchorTimestamp).toBe(4000);
		expect(playbackTimeline.derivePosition(ticked.position, 4000)).toBe(3);
	});

	it('does not advance while paused', () => {
		const playing = playbackTimeline.play(IDLE_PLAYBACK, 'song-1', 1000);
		const paused = playbackTimeline.pause(playing, 4000);

		const ticked = playbackTimeline.tick(paused, [], 9000);

		expect(ticked).toEqual(paused);
	});
});
