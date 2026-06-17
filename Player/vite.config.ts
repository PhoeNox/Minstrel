import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

const backend = process.env['services__backend__http__0'] ?? 'http://localhost:5000';
const port = process.env.PORT ? Number(process.env.PORT) : 5173;

export default defineConfig({
	server: {
		port,
		strictPort: !!process.env.PORT,
		fs: { allow: ['..'] },
		proxy: {
			'/playback': { target: backend, changeOrigin: true },
			'/timer': { target: backend, changeOrigin: true },
			'/system': backend
		}
	},
	plugins: [sveltekit()]
});
