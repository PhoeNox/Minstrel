<script lang="ts">
	import { onMount } from 'svelte';
	import { browserMusicStore, type MusicStore, type SongRecord } from '$lib/musicStore';
	import { pickDirectoryFiles, supportsDirectoryPicker } from '$lib/importPicker';

	let store: MusicStore;
	let songs: SongRecord[] = $state([]);
	let evicted: SongRecord[] = $state([]);
	let evictedIds = $derived(new Set(evicted.map((song) => song.id)));
	let importing = $state(false);
	let error: string | null = $state(null);
	let notice: string | null = $state(null);
	let canPickFolder = $state(false);
	let fileInput: HTMLInputElement;

	onMount(async () => {
		store = browserMusicStore();
		canPickFolder = supportsDirectoryPicker();
		await refresh();
	});

	async function refresh(): Promise<void> {
		songs = await store.songs();
		evicted = await store.evictedSongs();
	}

	async function importFiles(files: File[]): Promise<void> {
		if (files.length === 0) return;
		importing = true;
		error = null;
		notice = null;
		try {
			const result = await store.importFiles(files);
			await refresh();
			notice = summarise(result.added.length, result.duplicates.length);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Import failed';
		} finally {
			importing = false;
		}
	}

	function summarise(added: number, duplicates: number): string {
		const songWord = (n: number) => (n === 1 ? 'song' : 'songs');
		if (duplicates === 0) return `Imported ${added} ${songWord(added)}.`;
		if (added === 0) return `${duplicates} ${songWord(duplicates)} already in the library.`;
		return `Imported ${added} ${songWord(added)}, ${duplicates} already present.`;
	}

	async function pickFolder(): Promise<void> {
		try {
			await importFiles(await pickDirectoryFiles());
		} catch (e) {
			if (e instanceof DOMException && e.name === 'AbortError') return;
			error = e instanceof Error ? e.message : 'Import failed';
		}
	}

	async function handleFiles(event: Event): Promise<void> {
		const input = event.currentTarget as HTMLInputElement;
		const files = [...(input.files ?? [])];
		input.value = '';
		await importFiles(files);
	}

	async function remove(song: SongRecord): Promise<void> {
		await store.remove(song.id);
		await refresh();
	}

	function formatLength(seconds: number): string {
		const minutes = Math.floor(seconds / 60);
		const remainder = Math.floor(seconds % 60);
		return `${minutes}:${remainder.toString().padStart(2, '0')}`;
	}
</script>

<div class="app">
	<header class="head">
		<span class="glyph">♪</span>
		<h1>Minstrel</h1>
		<p class="sub">Library</p>
	</header>

	<main class="deck">
		{#if evicted.length > 0}
			<div class="evicted" role="alert">
				{evicted.length} song{evicted.length === 1 ? '' : 's'} need re-importing — the phone cleared
				their audio. Import the same files to restore them.
			</div>
		{/if}

		{#if songs.length === 0}
			<p class="empty">No songs yet. Import some to begin.</p>
		{:else}
			<ul class="songs">
				{#each songs as song (song.id)}
					<li class="song" class:gone={evictedIds.has(song.id)}>
						<span class="details">
							<span class="title">{song.title}</span>
							<span class="artist">{song.artist}</span>
						</span>
						{#if evictedIds.has(song.id)}
							<span class="reimport">re-import</span>
						{:else}
							<span class="length">{formatLength(song.length)}</span>
						{/if}
						<button class="remove" aria-label="Remove" onclick={() => remove(song)}>✕</button>
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
			multiple
			onchange={handleFiles}
		/>
		{#if canPickFolder}
			<button class="import" disabled={importing} onclick={pickFolder}>
				{importing ? 'Importing…' : 'Import a folder'}
			</button>
		{:else}
			<button class="import" disabled={importing} onclick={() => fileInput.click()}>
				{importing ? 'Importing…' : 'Import songs'}
			</button>
		{/if}
	</footer>

	{#if error}
		<div class="toast" role="alert">{error}</div>
	{:else if notice}
		<div class="toast notice" role="status">{notice}</div>
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

	.song.gone {
		opacity: 0.7;
		border-color: rgba(226, 59, 52, 0.4);
	}

	.details {
		flex: 1;
		min-width: 0;
		display: flex;
		flex-direction: column;
		gap: 0.12rem;
	}

	.title {
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
		font-size: 1.05rem;
		color: var(--text);
	}

	.artist {
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
		font-size: 0.88rem;
		color: var(--dim);
	}

	.length {
		flex: none;
		font-size: 0.85rem;
		font-variant-numeric: tabular-nums;
		color: var(--muted);
	}

	.reimport {
		flex: none;
		font-size: 0.78rem;
		letter-spacing: 0.04em;
		color: var(--blood-bright);
	}

	.remove {
		flex: none;
		width: 2.4rem;
		height: 2.4rem;
		border: 1px solid var(--panel-edge);
		border-radius: 9px;
		background: #1a1413;
		color: var(--muted);
		font-size: 0.85rem;
		cursor: pointer;
	}

	.remove:active {
		color: var(--blood-bright);
		border-color: rgba(226, 59, 52, 0.4);
	}

	.evicted {
		margin-bottom: 0.9rem;
		padding: 0.7rem 0.85rem;
		border: 1px solid rgba(226, 59, 52, 0.5);
		border-radius: 11px;
		background: linear-gradient(180deg, #2a1413, #1c0f0e);
		color: #f3d9d4;
		font-size: 0.9rem;
		line-height: 1.35;
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

	.toast.notice {
		border-color: rgba(227, 193, 119, 0.4);
		background: linear-gradient(180deg, #221a10, #161009);
		color: var(--accent);
	}
</style>
