import { describe, expect, it } from 'vitest';
import { deriveTimeLeft, formatTimeLeft } from '$shared/timer';

describe('deriveTimeLeft', () => {
	it('counts down with elapsed time while running', () => {
		const anchor = { running: true, anchorTimestamp: 1000, durationLeftAtAnchor: 60 };

		expect(deriveTimeLeft(anchor, 11_000)).toBe(50);
	});

	it('is exactly the duration at the anchor instant', () => {
		const anchor = { running: true, anchorTimestamp: 1000, durationLeftAtAnchor: 60 };

		expect(deriveTimeLeft(anchor, 1000)).toBe(60);
	});

	it('never goes below zero', () => {
		const anchor = { running: true, anchorTimestamp: 1000, durationLeftAtAnchor: 60 };

		expect(deriveTimeLeft(anchor, 100_000)).toBe(0);
	});

	it('stays at the duration while not running', () => {
		const anchor = { running: false, anchorTimestamp: 1000, durationLeftAtAnchor: 42 };

		expect(deriveTimeLeft(anchor, 100_000)).toBe(42);
	});
});

describe('formatTimeLeft', () => {
	it('formats whole minutes and seconds', () => {
		expect(formatTimeLeft(125)).toBe('2:05');
	});

	it('rounds partial seconds up', () => {
		expect(formatTimeLeft(59.2)).toBe('1:00');
	});

	it('formats zero', () => {
		expect(formatTimeLeft(0)).toBe('0:00');
	});
});
