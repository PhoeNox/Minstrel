import type { Phase } from './state';

export class CommandError extends Error {
	constructor(readonly action: string) {
		super(`${action} failed`);
		this.name = 'CommandError';
	}
}

export function play(): Promise<void> {
	return send('Play', '/playback/play');
}

export function pause(): Promise<void> {
	return send('Pause', '/playback/pause');
}

export function switchPhase(): Promise<void> {
	return send('Switch phase', '/playback/switch-phase');
}

export function selectSong(phase: Phase, index: number): Promise<void> {
	return send('Select song', '/playback/select', { phase, index });
}

export function addSong(phase: Phase, songId: string): Promise<void> {
	return send('Add song', '/playlist/add', { phase, songId });
}

export function removeSong(phase: Phase, index: number): Promise<void> {
	return send('Remove song', '/playlist/remove', { phase, index });
}

export function shuffle(phase: Phase): Promise<void> {
	return send('Shuffle', '/playlist/shuffle', { phase });
}

export function moveSong(phase: Phase, oldIndex: number, newIndex: number): Promise<void> {
	return send('Move song', '/playlist/move', { phase, oldIndex, newIndex });
}

export function setGain(phase: Phase, value: number): Promise<void> {
	return send('Set volume', '/playback/set-gain', { phase, value });
}

export function startTimer(duration: number): Promise<void> {
	return send('Start timer', '/timer/start', { duration });
}

export function stopTimer(): Promise<void> {
	return send('Stop timer', '/timer/stop');
}

async function send(action: string, url: string, body?: unknown): Promise<void> {
	const init: RequestInit =
		body === undefined
			? { method: 'POST' }
			: { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) };

	let response: Response;
	try {
		response = await fetch(url, init);
	} catch {
		throw new CommandError(action);
	}
	if (!response.ok) {
		throw new CommandError(action);
	}
}
