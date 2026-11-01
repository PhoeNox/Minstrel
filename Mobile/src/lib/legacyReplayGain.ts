import type { PersistedPlaylist, PersistedSession } from './musicStore';

// Songs and Playlist entries stored before ReplayGain was read lack the field; reading it
// as null keeps them playing uncorrected rather than at a NaN gain.
export function withReplayGain<T extends { replayGain: number | null }>(stored: T): T {
	return { ...stored, replayGain: stored.replayGain ?? null };
}

export function sessionWithReplayGain(session: PersistedSession): PersistedSession {
	return {
		...session,
		day: playlistWithReplayGain(session.day),
		night: playlistWithReplayGain(session.night)
	};
}

function playlistWithReplayGain(playlist: PersistedPlaylist): PersistedPlaylist {
	return { ...playlist, entries: playlist.entries.map(withReplayGain) };
}
