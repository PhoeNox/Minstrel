export function replayGainFactor(decibels: number | null): number {
	return decibels === null ? 1 : 10 ** (Math.min(decibels, 0) / 20);
}
