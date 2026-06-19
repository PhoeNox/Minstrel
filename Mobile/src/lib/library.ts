import type { SongRecord } from './musicStore';

export interface ImportPartition {
	added: SongRecord[];
	duplicates: SongRecord[];
}

// Splits freshly-read candidates into songs new to the Library and songs already
// present (same content id) — including duplicates within the one batch. Re-importing
// the same files lands entirely in `duplicates`, so the Library is restored without
// growing.
export function partitionNew(existingIds: Set<string>, candidates: SongRecord[]): ImportPartition {
	const added: SongRecord[] = [];
	const duplicates: SongRecord[] = [];
	const seen = new Set(existingIds);
	for (const candidate of candidates) {
		if (seen.has(candidate.id)) {
			duplicates.push(candidate);
		} else {
			added.push(candidate);
			seen.add(candidate.id);
		}
	}
	return { added, duplicates };
}

// The known songs whose audio bytes are gone from storage (OPFS eviction). Surfaced
// so the Storyteller re-imports before the game rather than hitting silence mid-session.
export function findEvicted(records: SongRecord[], presentBytes: Set<string>): SongRecord[] {
	return records.filter((record) => !presentBytes.has(record.id));
}
