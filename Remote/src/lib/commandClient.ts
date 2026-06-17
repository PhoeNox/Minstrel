import type { Phase } from './state';

export async function play(): Promise<void> {
	await fetch('/playback/play', { method: 'POST' });
}

export async function pause(): Promise<void> {
	await fetch('/playback/pause', { method: 'POST' });
}

export async function switchPhase(): Promise<void> {
	await fetch('/playback/switch-phase', { method: 'POST' });
}

export async function selectSong(phase: Phase, index: number): Promise<void> {
	await post('/playback/select', { phase, index });
}

export async function addSong(phase: Phase, songId: string): Promise<void> {
	await post('/playlist/add', { phase, songId });
}

export async function removeSong(phase: Phase, index: number): Promise<void> {
	await post('/playlist/remove', { phase, index });
}

export async function shuffle(phase: Phase): Promise<void> {
	await post('/playlist/shuffle', { phase });
}

export async function moveSong(phase: Phase, oldIndex: number, newIndex: number): Promise<void> {
	await post('/playlist/move', { phase, oldIndex, newIndex });
}

export async function setGain(phase: Phase, value: number): Promise<void> {
	await post('/playback/set-gain', { phase, value });
}

export async function startTimer(duration: number): Promise<void> {
	await post('/timer/start', { duration });
}

export async function stopTimer(): Promise<void> {
	await fetch('/timer/stop', { method: 'POST' });
}

async function post(url: string, body: unknown): Promise<void> {
	await fetch(url, {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify(body)
	});
}
