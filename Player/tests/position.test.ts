import { describe, expect, it } from 'vitest';
import { derivePosition } from '$shared/position';

describe('derivePosition', () => {
	it('advances with elapsed time while playing', () => {
		const anchor = { songId: 'X', offset: 10, anchorTimestamp: 2000, isPlaying: true };

		expect(derivePosition(anchor, 4500)).toBe(12.5);
	});

	it('stays frozen at the offset while paused', () => {
		const anchor = { songId: 'X', offset: 10, anchorTimestamp: 2000, isPlaying: false };

		expect(derivePosition(anchor, 9000)).toBe(10);
	});

	it('is exactly the offset at the anchor instant', () => {
		const anchor = { songId: 'X', offset: 7, anchorTimestamp: 5000, isPlaying: true };

		expect(derivePosition(anchor, 5000)).toBe(7);
	});

	it('starts from zero offset for a freshly started song', () => {
		const anchor = { songId: 'X', offset: 0, anchorTimestamp: 1000, isPlaying: true };

		expect(derivePosition(anchor, 1000)).toBe(0);
	});
});
