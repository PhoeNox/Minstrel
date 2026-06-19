import { describe, expect, it } from 'vitest';
import * as playbackTimeline from '$shared/core/playbackTimeline';
import { IDLE_PLAYBACK } from '$shared/core';

describe('resume', () => {
	it('continues from the frozen offset', () => {
		const playing = playbackTimeline.play(IDLE_PLAYBACK, 'song-1', 1000);
		const paused = playbackTimeline.pause(playing, 4000);

		const resumed = playbackTimeline.resume(paused, 10000);

		expect(resumed.position.isPlaying).toBe(true);
		expect(playbackTimeline.derivePosition(resumed.position, 12000)).toBe(5);
	});

	it('does nothing when no song is current', () => {
		const resumed = playbackTimeline.resume(IDLE_PLAYBACK, 1000);

		expect(resumed.position.isPlaying).toBe(false);
		expect(resumed.position.songId).toBeNull();
	});
});
