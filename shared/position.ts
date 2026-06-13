export interface PositionAnchor {
	songId: string | null;
	offset: number;
	anchorTimestamp: number;
	isPlaying: boolean;
}

export function derivePosition(anchor: PositionAnchor, now: number): number {
	return anchor.isPlaying ? anchor.offset + (now - anchor.anchorTimestamp) / 1000 : anchor.offset;
}
