<script lang="ts">
	import { onMount } from 'svelte';
	import { browserMusicStore, type MusicStore, type SongRecord } from '$lib/musicStore';

	let store: MusicStore;
	let songs: SongRecord[] = $state([]);
	let importing = $state(false);
	let error: string | null = $state(null);
	let fileInput: HTMLInputElement;

	onMount(async () => {
		store = browserMusicStore();
		songs = await store.songs();
	});

	async function handleFiles(event: Event): Promise<void> {
		const input = event.currentTarget as HTMLInputElement;
		const file = input.files?.[0];
		input.value = '';
		if (!file) return;

		importing = true;
		error = null;
		try {
			await store.importSong(file);
			songs = await store.songs();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Import failed';
		} finally {
			importing = false;
		}
	}

	async function play(song: SongRecord): Promise<void> {
		const blob = await store.audioBlob(song.id);
		if (!blob) {
			error = `"${song.name}" is no longer stored — re-import it.`;
			return;
		}
		const url = URL.createObjectURL(blob);
		const audio = new Audio(url);
		audio.addEventListener('ended', () => URL.revokeObjectURL(url));
		void audio.play();
	}
</script>

<div class="app">
	<header class="head">
		<span class="glyph">♪</span>
		<h1>Minstrel</h1>
		<p class="sub">Library</p>
	</header>

	<main class="deck">
		{#if songs.length === 0}
			<p class="empty">No songs yet. Import one to begin.</p>
		{:else}
			<ul class="songs">
				{#each songs as song (song.id)}
					<li class="song">
						<button class="play" aria-label="Play" onclick={() => play(song)}>▶</button>
						<span class="name">{song.name}</span>
					</li>
				{/each}
			</ul>
		{/if}
	</main>

	<footer class="controls">
		<input
			bind:this={fileInput}
			class="hidden-input"
			type="file"
			accept="audio/*"
			onchange={handleFiles}
		/>
		<button class="import" disabled={importing} onclick={() => fileInput.click()}>
			{importing ? 'Importing…' : 'Import a song'}
		</button>
	</footer>

	{#if error}
		<div class="toast" role="alert">{error}</div>
	{/if}
</div>

<style>
	:global(:root) {
		--ink: #0a0608;
		--panel: #15100f;
		--panel-edge: rgba(239, 230, 211, 0.1);
		--text: #efe6d3;
		--dim: #b9ac93;
		--muted: #7c7263;
		--blood: #c01f1f;
		--blood-bright: #e23b34;
		--accent: #e3c177;
		--accent-rgb: 227, 193, 119;
		--display: 'Cinzel', 'Times New Roman', serif;
		--body: 'EB Garamond', Georgia, serif;
	}

	:global(body) {
		color: var(--text);
		font-family: var(--body);
		-webkit-font-smoothing: antialiased;
	}

	:global(*) {
		-webkit-tap-highlight-color: transparent;
	}

	.app {
		position: fixed;
		inset: 0;
		display: flex;
		flex-direction: column;
		background:
			radial-gradient(120% 70% at 50% -10%, rgba(var(--accent-rgb), 0.08), transparent 60%),
			linear-gradient(180deg, #100b0a 0%, var(--ink) 60%);
		overflow: hidden;
	}

	.head {
		flex: none;
		padding: calc(env(safe-area-inset-top) + 1rem) 1rem 0.9rem;
		text-align: center;
		border-bottom: 1px solid rgba(239, 230, 211, 0.08);
	}

	.glyph {
		font-size: 1.6rem;
		color: var(--accent);
	}

	.head h1 {
		margin: 0.2rem 0 0;
		font-family: var(--display);
		font-weight: 700;
		font-size: 1.5rem;
		letter-spacing: 0.12em;
		color: var(--text);
	}

	.sub {
		margin: 0.15rem 0 0;
		font-size: 0.72rem;
		letter-spacing: 0.22em;
		text-transform: uppercase;
		color: var(--muted);
	}

	.deck {
		flex: 1;
		min-height: 0;
		overflow-y: auto;
		-webkit-overflow-scrolling: touch;
		padding: 1rem;
	}

	.empty {
		margin-top: 3rem;
		text-align: center;
		color: var(--muted);
		font-style: italic;
	}

	.songs {
		list-style: none;
		margin: 0;
		padding: 0;
		display: flex;
		flex-direction: column;
		gap: 0.55rem;
	}

	.song {
		display: flex;
		align-items: center;
		gap: 0.7rem;
		padding: 0.7rem 0.8rem;
		border: 1px solid var(--panel-edge);
		border-radius: 12px;
		background: var(--panel);
	}

	.play {
		flex: none;
		width: 2.4rem;
		height: 2.4rem;
		border: 1px solid var(--panel-edge);
		border-radius: 9px;
		background: #1a1413;
		color: var(--accent);
		font-size: 0.85rem;
		cursor: pointer;
	}

	.name {
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
		font-size: 1.05rem;
	}

	.controls {
		flex: none;
		padding: 0.9rem 1rem calc(env(safe-area-inset-bottom) + 0.9rem);
		border-top: 1px solid rgba(239, 230, 211, 0.08);
	}

	.hidden-input {
		display: none;
	}

	.import {
		width: 100%;
		min-height: 3.5rem;
		border: none;
		border-radius: 13px;
		background: linear-gradient(180deg, var(--blood-bright), var(--blood) 60%, #7d0f0f);
		color: #fff5ef;
		font-family: var(--display);
		font-weight: 700;
		font-size: 1.1rem;
		letter-spacing: 0.06em;
		cursor: pointer;
		box-shadow: 0 12px 26px -12px rgba(192, 31, 31, 0.75);
	}

	.import:disabled {
		opacity: 0.55;
		cursor: default;
	}

	.toast {
		position: fixed;
		left: 50%;
		bottom: calc(env(safe-area-inset-bottom) + 5.5rem);
		transform: translateX(-50%);
		max-width: calc(100% - 2rem);
		padding: 0.7rem 1.1rem;
		border: 1px solid rgba(226, 59, 52, 0.5);
		border-radius: 11px;
		background: linear-gradient(180deg, #2a1413, #1c0f0e);
		color: #f3d9d4;
		font-size: 0.95rem;
		text-align: center;
	}
</style>
