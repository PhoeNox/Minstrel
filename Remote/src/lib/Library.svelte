<script lang="ts">
	import type { SongDto } from './state';

	let { songs }: { songs: SongDto[] } = $props();

	let search = $state('');

	const filtered = $derived(
		songs.filter((song) => {
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
	<h2>Library</h2>
	<input class="search" type="search" placeholder="Search…" bind:value={search} />
	<ul>
		{#each filtered as song (song.id)}
			<li>
				<span class="details">
					<span class="title">{song.title}</span>
					<span class="artist">{song.artist}</span>
				</span>
				<span class="length">{formatLength(song.length)}</span>
			</li>
		{/each}
	</ul>
</section>

<style>
	section {
		text-align: left;
		margin-top: 2rem;
	}

	h2 {
		font-size: 1rem;
		text-transform: uppercase;
		letter-spacing: 0.05em;
		color: #777;
	}

	.search {
		width: 100%;
		box-sizing: border-box;
		font-size: 1rem;
		padding: 0.5rem;
		margin-bottom: 0.5rem;
	}

	ul {
		list-style: none;
		margin: 0;
		padding: 0;
	}

	li {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		padding: 0.5rem 0.25rem;
		border-bottom: 1px solid #eee;
	}

	.details {
		flex: 1;
		display: flex;
		flex-direction: column;
		gap: 0.1rem;
	}

	.title {
		font-weight: 600;
	}

	.artist {
		font-size: 0.85rem;
		color: #777;
	}

	.length {
		font-variant-numeric: tabular-nums;
		color: #999;
	}
</style>
