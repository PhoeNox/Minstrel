// TypeScript mirror of the C# `Core` record shapes (ADR-0007/0008). Pure data, no
// DOM or storage. The C# `Core` and this module are two sources of the same domain
// rules; change a rule in both, kept honest by the parity suite.

export type GamePhase = 'Day' | 'Night';

export interface Track {
	id: string;
	length: number;
}

export interface PlaylistEntry {
	id: string;
	path: string;
	title: string;
	artist: string;
	length: number;
}

export interface PositionAnchor {
	songId: string | null;
	offset: number;
	anchorTimestamp: number;
	isPlaying: boolean;
}

export interface PhasePlayback {
	cursor: number | null;
	gain: number;
	resumeOffset: number;
}

export interface PlaybackState {
	activePhase: GamePhase;
	day: PhasePlayback;
	night: PhasePlayback;
	position: PositionAnchor;
}

export interface TimerAnchor {
	running: boolean;
	anchorTimestamp: number;
	durationLeftAtAnchor: number;
}

export const IDLE_POSITION: PositionAnchor = {
	songId: null,
	offset: 0,
	anchorTimestamp: 0,
	isPlaying: false
};

export const IDLE_PHASE_PLAYBACK: PhasePlayback = { cursor: null, gain: 1.0, resumeOffset: 0 };

export const IDLE_PLAYBACK: PlaybackState = {
	activePhase: 'Day',
	day: IDLE_PHASE_PLAYBACK,
	night: IDLE_PHASE_PLAYBACK,
	position: IDLE_POSITION
};

export const IDLE_TIMER: TimerAnchor = { running: false, anchorTimestamp: 0, durationLeftAtAnchor: 0 };
