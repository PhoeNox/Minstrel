import adapter from '@sveltejs/adapter-static';
import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	preprocess: vitePreprocess(),
	kit: {
		adapter: adapter({
			pages: '../Backend/wwwroot/remote',
			assets: '../Backend/wwwroot/remote',
			fallback: 'index.html'
		}),
		paths: {
			// BASE_PATH is the bare segment (e.g. "remote"); the leading slash is added here so the
			// build recipe never passes a POSIX-looking path that Git Bash mangles on Windows.
			base: process.env.BASE_PATH ? '/' + process.env.BASE_PATH : ''
		},
		alias: {
			$shared: '../shared'
		}
	}
};

export default config;
