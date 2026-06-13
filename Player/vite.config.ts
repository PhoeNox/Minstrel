import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
	plugins: [sveltekit()],
	server: {
		proxy: {
			'/sse': { target: 'http://localhost:5000', changeOrigin: true },
			'/audio': 'http://localhost:5000',
			'/commands': 'http://localhost:5000'
		}
	}
});
