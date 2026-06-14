import type { Phase } from './state';

export async function play(): Promise<void> {
	await fetch('/commands/play', { method: 'POST' });
}

export async function pause(): Promise<void> {
	await fetch('/commands/pause', { method: 'POST' });
}

export async function switchPhase(): Promise<void> {
	await fetch('/commands/switch-phase', { method: 'POST' });
}

export async function selectSong(phase: Phase, index: number): Promise<void> {
	await post('/commands/select', { phase, index });
}

export async function moveSong(phase: Phase, oldIndex: number, newIndex: number): Promise<void> {
	await post('/commands/move', { phase, oldIndex, newIndex });
}

export async function setGain(phase: Phase, value: number): Promise<void> {
	await post('/commands/set-gain', { phase, value });
}

export async function startTimer(duration: number): Promise<void> {
	await post('/commands/timer-start', { duration });
}

export async function stopTimer(): Promise<void> {
	await fetch('/commands/timer-stop', { method: 'POST' });
}

async function post(url: string, body: unknown): Promise<void> {
	await fetch(url, {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify(body)
	});
}
