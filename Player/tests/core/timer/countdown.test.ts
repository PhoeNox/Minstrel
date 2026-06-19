import { describe, expect, it } from 'vitest';
import * as countdownTimer from '$shared/core/countdownTimer';
import { IDLE_TIMER } from '$shared/core';

describe('start / deriveTimeLeft', () => {
	it('counts down with elapsed time while running', () => {
		const anchor = countdownTimer.start(60, 1000);

		expect(countdownTimer.deriveTimeLeft(anchor, 11_000)).toBe(50);
	});

	it('is exactly the duration at the anchor instant', () => {
		const anchor = countdownTimer.start(60, 1000);

		expect(countdownTimer.deriveTimeLeft(anchor, 1000)).toBe(60);
	});

	it('never goes below zero', () => {
		const anchor = countdownTimer.start(60, 1000);

		expect(countdownTimer.deriveTimeLeft(anchor, 100_000)).toBe(0);
	});

	it('stays at the duration while idle', () => {
		expect(countdownTimer.deriveTimeLeft(IDLE_TIMER, 100_000)).toBe(0);
	});
});

describe('hasExpired', () => {
	it('has not expired before the duration elapses', () => {
		const anchor = countdownTimer.start(60, 1000);

		expect(countdownTimer.hasExpired(anchor, 60_000)).toBe(false);
	});

	it('expires once the duration elapses', () => {
		const anchor = countdownTimer.start(60, 1000);

		expect(countdownTimer.hasExpired(anchor, 61_000)).toBe(true);
	});

	it('an idle timer never expires', () => {
		expect(countdownTimer.hasExpired(IDLE_TIMER, 100_000)).toBe(false);
	});
});

describe('formatTimeLeft', () => {
	it('formats whole minutes and seconds', () => {
		expect(countdownTimer.formatTimeLeft(125)).toBe('2:05');
	});

	it('rounds partial seconds up', () => {
		expect(countdownTimer.formatTimeLeft(59.2)).toBe('1:00');
	});

	it('formats zero', () => {
		expect(countdownTimer.formatTimeLeft(0)).toBe('0:00');
	});
});
