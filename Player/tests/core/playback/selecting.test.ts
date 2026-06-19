import { describe, expect, it } from 'vitest';
import * as playbackTimeline from '$shared/core/playbackTimeline';
import { IDLE_PLAYBACK, type PlaybackState } from '$shared/core';
import { track } from '../fixtures';

const withCursors = (): PlaybackState => ({
	...IDLE_PLAYBACK,
	day: { cursor: 0, gain: 1.0, resumeOffset: 0 },
	night: { cursor: 0, gain: 1.0, resumeOffset: 0 }
});

describe('select', () => {
	it('selecting in the active phase starts playing that song', () => {
		const result = playbackTimeline.select(withCursors(), 'Day', 1, track('day-2', 10), 1000);

		expect(result.position.songId).toBe('day-2');
		expect(result.day.cursor).toBe(1);
		expect(result.position.isPlaying).toBe(true);
		expect(result.position.offset).toBe(0);
	});

	it('selecting in the inactive phase updates its current song without sounding', () => {
		const result = playbackTimeline.select(withCursors(), 'Night', 1, track('night-2', 10), 1000);

		expect(result.night.cursor).toBe(1);
		expect(result.position.songId).toBeNull();
		expect(result.position.isPlaying).toBe(false);
	});
});
