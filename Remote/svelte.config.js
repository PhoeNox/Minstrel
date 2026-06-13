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
			base: process.env.BASE_PATH ?? ''
		},
		alias: {
			$shared: '../shared'
		}
	}
};

export default config;
