import { describe, expect, it } from 'vitest';
import { replayGainFactor } from 'minstrel-shared/replayGain';

describe('replayGainFactor', () => {
	it('leaves a song without ReplayGain uncorrected', () => {
		expect(replayGainFactor(null)).toBe(1);
	});

	it('leaves a song at 0 dB uncorrected', () => {
		expect(replayGainFactor(0)).toBe(1);
	});

	it('attenuates a song with negative ReplayGain', () => {
		expect(replayGainFactor(-6)).toBeCloseTo(0.501187, 6);
	});

	it('never amplifies a song with positive ReplayGain', () => {
		expect(replayGainFactor(4)).toBe(1);
	});
});
