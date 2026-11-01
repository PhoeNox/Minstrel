import adapter from '@sveltejs/adapter-static';
import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

const port = process.env.PORT ? Number(process.env.PORT) : 5175;

export default defineConfig({
	server: {
		port,
		strictPort: !!process.env.PORT,
		// Bind every interface when a port is pinned so a phone on the LAN can reach
		// the dev server for real-device testing.
		host: process.env.PORT ? true : undefined,
		fs: { allow: ['..'] }
	},
	plugins: [
		sveltekit({
			preprocess: vitePreprocess(),
			// Standalone static PWA (ADR-0008): no Backend to serve it, hosted on a free
			// HTTPS host. fallback gives a single-page-app shell the service worker caches.
			adapter: adapter({ pages: 'build', assets: 'build', fallback: 'index.html' }),
			paths: {
				// BASE_PATH is the bare host subpath (e.g. a GitHub Pages project path); the
				// leading slash is added here so the build recipe never passes a POSIX-looking
				// path that Git Bash mangles on Windows.
				base: process.env.BASE_PATH ? '/' + process.env.BASE_PATH : ''
			}
		})
	]
});
