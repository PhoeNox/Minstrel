import { writable, type Readable } from 'svelte/store';

// A command the user issued that the system rejected (e.g. the Backend returned non-OK,
// or it was unreachable). Distinct from a programming bug: a CommandError is shown to the
// user as a transient message, anything else propagates. Mobile's in-process commands do
// not reject this way, so its surface simply never raises one.
export class CommandError extends Error {
	constructor(readonly action: string) {
		super(`${action} failed`);
		this.name = 'CommandError';
	}
}

const CLEAR_AFTER_MS = 4000;

const store = writable<string | null>(null);
let timeout: ReturnType<typeof setTimeout> | undefined;

// The latest failed-command message, or null when nothing has failed recently.
export const commandError: Readable<string | null> = store;

// Run a command, surfacing a failed one as a transient message. A non-CommandError
// signals a bug rather than a rejected command, so it propagates rather than being shown.
export async function report(command: Promise<void>): Promise<void> {
	try {
		await command;
	} catch (error) {
		if (!(error instanceof CommandError)) {
			throw error;
		}
		show(error.message);
	}
}

function show(message: string): void {
	store.set(message);
	clearTimeout(timeout);
	timeout = setTimeout(() => store.set(null), CLEAR_AFTER_MS);
}
