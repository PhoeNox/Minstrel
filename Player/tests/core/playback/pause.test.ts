import { describe, expect, it } from 'vitest';
import * as playbackTimeline from '$shared/core/playbackTimeline';
import { IDLE_PLAYBACK } from '$shared/core';

describe('pause', () => {
	it('freezes the anchor at the elapsed offset', () => {
		const playing = playbackTimeline.play(IDLE_PLAYBACK, 'song-1', 1000);

		const paused = playbackTimeline.pause(playing, 4000);

		expect(paused.position.isPlaying).toBe(false);
		expect(paused.position.offset).toBe(3);
	});

	it('frozen position does not advance as time passes', () => {
		const playing = playbackTimeline.play(IDLE_PLAYBACK, 'song-1', 1000);
		const paused = playbackTimeline.pause(playing, 4000);

		const laterPosition = playbackTimeline.derivePosition(paused.position, 9000);

		expect(laterPosition).toBe(3);
	});
});
