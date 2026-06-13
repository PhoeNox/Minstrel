export async function play(): Promise<void> {
	await fetch('/commands/play', { method: 'POST' });
}

export async function pause(): Promise<void> {
	await fetch('/commands/pause', { method: 'POST' });
}
