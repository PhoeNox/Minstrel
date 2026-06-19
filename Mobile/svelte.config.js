import adapter from '@sveltejs/adapter-static';
import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	preprocess: vitePreprocess(),
	kit: {
		// Standalone static PWA (ADR-0008): no Backend to serve it, hosted on a free
		// HTTPS host. fallback gives a single-page-app shell the service worker caches.
		adapter: adapter({
			pages: 'build',
			assets: 'build',
			fallback: 'index.html'
		}),
		paths: {
			// BASE_PATH is the bare host subpath (e.g. a GitHub Pages project path); the
			// leading slash is added here so the build recipe never passes a POSIX-looking
			// path that Git Bash mangles on Windows.
			base: process.env.BASE_PATH ? '/' + process.env.BASE_PATH : ''
		},
		alias: {
			$shared: '../shared'
		}
	}
};

export default config;
