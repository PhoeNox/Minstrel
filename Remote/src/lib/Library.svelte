<script lang="ts">
	import { report } from '$shared/ui/commandError';
	import type { MinstrelApi } from '$shared/ui/minstrelApi';
	import type { PlaylistsDto, SongDto } from '$shared/ui/state';
	import { unusedSongs } from '$shared/ui/unused';

	let {
		api,
		songs,
		playlists
	}: { api: MinstrelApi; songs: SongDto[]; playlists: PlaylistsDto } = $props();

	let search = $state('');
	let unusedOnly = $state(false);

	const searchable = $derived(unusedOnly ? unusedSongs(songs, playlists) : songs);

	const filtered = $derived(
		searchable.filter((song) => {
			const query = search.toLowerCase();
			return (
				song.artist.toLowerCase().includes(query) || song.title.toLowerCase().includes(query)
			);
		})
	);

	function formatLength(seconds: number): string {
		const minutes = Math.floor(seconds / 60);
		const remainder = Math.floor(seconds % 60);
		return `${minutes}:${remainder.toString().padStart(2, '0')}`;
	}
</script>

<section>
	<div class="searchbar">
		<span class="search-glyph">⌕</span>
		<input class="search" type="search" placeholder="Search the library…" bind:value={search} />
		<button
			class="chip"
			class:on={unusedOnly}
			aria-pressed={unusedOnly}
			onclick={() => (unusedOnly = !unusedOnly)}>Unused</button
		>
		<span class="result-count">{filtered.length}</span>
	</div>

	{#if filtered.length === 0}
		<p class="empty">
			{songs.length === 0
				? 'The library is empty.'
				: unusedOnly && search === ''
					? 'Every song is already in a playlist.'
					: 'No matches.'}
		</p>
	{:else}
		<ul>
			{#each filtered as song (song.id)}
				<li>
					<span class="details">
						<span class="title">{song.title}</span>
						<span class="artist">{song.artist}</span>
					</span>
					<span class="length">{formatLength(song.length)}</span>
					<button
						class="add day"
						title="Add to Day playlist"
						onclick={() => report(api.addSong('Day', song.id))}>☀</button
					>
					<button
						class="add night"
						title="Add to Night playlist"
						onclick={() => report(api.addSong('Night', song.id))}>☾</button
					>
				</li>
			{/each}
		</ul>
	{/if}
</section>

<style>
	.searchbar {
		position: sticky;
		top: -0.85rem;
		z-index: 1;
		display: flex;
		align-items: center;
		gap: 0.55rem;
		padding: 0.6rem 0.75rem;
		margin: -0.1rem 0 0.75rem;
		border: 1px solid var(--line);
		border-radius: 11px;
		background: #18120f;
	}

	.search-glyph {
		font-size: 1.1rem;
		color: var(--muted);
	}

	.search {
		flex: 1;
		min-width: 0;
		border: none;
		background: none;
		color: var(--text);
		font-family: var(--body);
		font-size: 1.05rem;
		outline: none;
	}

	.search::placeholder {
		color: var(--muted);
	}

	.search::-webkit-search-cancel-button {
		filter: grayscale(1) opacity(0.5);
	}

	.chip {
		flex: none;
		font-family: var(--body);
		font-size: 0.82rem;
		color: var(--muted);
		background: none;
		border: 1px solid var(--line);
		padding: 0.12rem 0.55rem;
		border-radius: 999px;
		cursor: pointer;
	}

	.chip.on {
		color: var(--accent);
		border-color: rgba(var(--accent-rgb), 0.4);
		background: rgba(var(--accent-rgb), 0.12);
	}

	.result-count {
		font-size: 0.82rem;
		font-variant-numeric: tabular-nums;
		color: var(--muted);
		background: var(--row);
		padding: 0.12rem 0.55rem;
		border-radius: 999px;
	}

	.empty {
		margin: 0;
		padding: 1.6rem 0.5rem;
		text-align: center;
		font-style: italic;
		color: var(--muted);
	}

	ul {
		list-style: none;
		margin: 0;
		padding: 0;
		display: flex;
		flex-direction: column;
		gap: 0.4rem;
	}

	li {
		display: flex;
		align-items: center;
		gap: 0.4rem;
		padding: 0.45rem 0.5rem 0.45rem 0.7rem;
		border: 1px solid var(--row-edge);
		border-radius: 11px;
		background: var(--row);
	}

	.details {
		flex: 1;
		min-width: 0;
		display: flex;
		flex-direction: column;
		gap: 0.1rem;
	}

	.title {
		font-size: 1.1rem;
		font-weight: 600;
		color: var(--text);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.artist {
		font-size: 0.9rem;
		color: var(--dim);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.length {
		flex: none;
		font-size: 0.85rem;
		font-variant-numeric: tabular-nums;
		color: var(--muted);
		margin-right: 0.15rem;
	}

	.add {
		flex: none;
		width: 2.6rem;
		height: 2.6rem;
		border-radius: 9px;
		border: 1px solid var(--line);
		background: rgba(0, 0, 0, 0.25);
		font-size: 1.05rem;
		cursor: pointer;
	}

	.add.day {
		color: #e3c177;
	}

	.add.day:active {
		background: rgba(227, 193, 119, 0.18);
		border-color: rgba(227, 193, 119, 0.4);
	}

	.add.night {
		color: #cdd6ea;
	}

	.add.night:active {
		background: rgba(205, 214, 234, 0.18);
		border-color: rgba(205, 214, 234, 0.4);
	}
</style>
