import type { Phase } from './state';

export async function play(): Promise<void> {
	await fetch('/commands/play', { method: 'POST' });
}

export async function pause(): Promise<void> {
	await fetch('/commands/pause', { method: 'POST' });
}

export async function selectSong(phase: Phase, songId: string): Promise<void> {
	await post('/commands/select', { phase, songId });
}

export async function moveSong(phase: Phase, oldIndex: number, newIndex: number): Promise<void> {
	await post('/commands/move', { phase, oldIndex, newIndex });
}

async function post(url: string, body: unknown): Promise<void> {
	await fetch(url, {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify(body)
	});
}
