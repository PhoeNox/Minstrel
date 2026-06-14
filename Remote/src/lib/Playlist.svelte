<script lang="ts">
	import { moveSong, selectSong } from './commandClient';
	import type { Phase, PlaylistDto } from './state';

	let { phase, playlist }: { phase: Phase; playlist: PlaylistDto } = $props();

	let dragIndex: number | null = $state(null);

	function formatLength(seconds: number): string {
		const minutes = Math.floor(seconds / 60);
		const remainder = Math.floor(seconds % 60);
		return `${minutes}:${remainder.toString().padStart(2, '0')}`;
	}

	function onDrop(targetIndex: number): void {
		if (dragIndex !== null && dragIndex !== targetIndex) {
			void moveSong(phase, dragIndex, targetIndex);
		}
		dragIndex = null;
	}
</script>

<section>
	<h2>{phase}</h2>
	<ul>
		{#each playlist.songs as song, index (song.id)}
			<li
				class:current={song.id === playlist.currentSongId}
				draggable="true"
				ondragstart={() => (dragIndex = index)}
				ondragover={(event) => event.preventDefault()}
				ondrop={() => onDrop(index)}
				ondragend={() => (dragIndex = null)}
			>
				<button class="select" onclick={() => selectSong(phase, song.id)}>
					<span class="title">{song.title}</span>
					<span class="artist">{song.artist}</span>
				</button>
				<span class="length">{formatLength(song.length)}</span>
			</li>
		{/each}
	</ul>
</section>

<style>
	section {
		text-align: left;
	}

	h2 {
		font-size: 1rem;
		text-transform: uppercase;
		letter-spacing: 0.05em;
		color: #777;
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
		cursor: grab;
	}

	li.current {
		background: #f3f0ff;
	}

	.select {
		flex: 1;
		display: flex;
		flex-direction: column;
		align-items: flex-start;
		gap: 0.1rem;
		background: none;
		border: none;
		padding: 0;
		font: inherit;
		text-align: left;
		cursor: pointer;
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
