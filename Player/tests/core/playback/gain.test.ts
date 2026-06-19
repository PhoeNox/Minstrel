import { describe, expect, it } from 'vitest';
import * as playbackTimeline from '$shared/core/playbackTimeline';
import { IDLE_PLAYBACK, type PlaybackState } from '$shared/core';
import { tracks } from '../fixtures';

const withCursors = (): PlaybackState => ({
	...IDLE_PLAYBACK,
	day: { cursor: 0, gain: 1.0, resumeOffset: 0 },
	night: { cursor: 0, gain: 1.0, resumeOffset: 0 }
});

describe('setGain', () => {
	it('updates the target phase gain', () => {
		const result = playbackTimeline.setGain(withCursors(), 'Night', 0.4);

		expect(result.night.gain).toBe(0.4);
	});

	it('leaves the other phase gain unchanged', () => {
		const result = playbackTimeline.setGain(withCursors(), 'Night', 0.4);

		expect(result.day.gain).toBe(1.0);
	});

	it('gain persists when the song advances within the phase', () => {
		const songs = tracks(['day-1', 10], ['day-2', 10]);
		const playing = playbackTimeline.play(
			{ ...IDLE_PLAYBACK, day: { cursor: 0, gain: 1.0, resumeOffset: 0 } },
			'day-1',
			1000
		);
		const quieter = playbackTimeline.setGain(playing, 'Day', 0.3);

		const advanced = playbackTimeline.tick(quieter, songs, 1000 + 11_000);

		expect(advanced.position.songId).toBe('day-2');
		expect(advanced.day.gain).toBe(0.3);
	});
});
