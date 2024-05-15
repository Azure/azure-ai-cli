import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [],
  server: {
    port: 3000
  },
  build: {
    outDir: 'dist',
    rollupOptions: {
      input: './src/index.ts'
    }
  }
});
