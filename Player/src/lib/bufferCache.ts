// Decoded AudioBuffers are large raw-float blobs and the Player runs unattended
// for hours, so the decoded-buffer cache is capped by estimated bytes: once the
// total exceeds the cap, least-recently-used entries are evicted. The retained
// set — the lead voice and the reconciler's warm targets — is never evicted,
// so an in-flight crossfade never loses a buffer it is about to need.

export const BUFFER_CACHE_BYTES = 256 * 1024 * 1024;

export interface CachedBuffer {
	songId: string;
	bytes: number;
}

export function evictions(order: CachedBuffer[], retain: Iterable<string>, limit: number): string[] {
	const total = order.reduce((sum, entry) => sum + entry.bytes, 0);
	if (total <= limit) {
		return [];
	}

	const retained = new Set(retain);
	const evicted: string[] = [];
	let freed = 0;
	for (const entry of order) {
		if (total - freed <= limit) {
			break;
		}
		if (retained.has(entry.songId)) {
			continue;
		}
		evicted.push(entry.songId);
		freed += entry.bytes;
	}

	return evicted;
}
