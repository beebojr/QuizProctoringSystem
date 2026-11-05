import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Frontend on :3001, API on :3000 (requests to /api are proxied).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3001,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://localhost:3000',
        changeOrigin: true,
      },
    },
  },
  build: {
    chunkSizeWarningLimit: 1500,
  },
})
