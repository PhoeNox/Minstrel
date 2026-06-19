// Stable content-derived id for an imported song. The C# `Core` hashes a song's
// filesystem path (SongId.cs); Mobile has no stable path — iOS hands over picked
// File objects with no directory — so it hashes the audio bytes instead. Content
// hashing also makes dedup and eviction recovery fall out for free: the same file
// re-imported (after eviction or reinstall) yields the same id the Playlist entries
// already reference. 16 hex chars, mirroring the C# id width.
export async function deriveSongId(bytes: ArrayBuffer): Promise<string> {
	const digest = await crypto.subtle.digest('SHA-256', bytes);
	return [...new Uint8Array(digest)]
		.map((byte) => byte.toString(16).padStart(2, '0'))
		.join('')
		.slice(0, 16);
}
