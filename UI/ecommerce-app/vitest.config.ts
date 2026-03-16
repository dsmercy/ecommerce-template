import { defineConfig } from 'vitest/config';
import path from 'path';

export default defineConfig({
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
  resolve: {
    alias: {
      '@':           path.resolve(__dirname, 'src'),
      '@api':        path.resolve(__dirname, 'src/api'),
      '@components': path.resolve(__dirname, 'src/components'),
      '@features':   path.resolve(__dirname, 'src/features'),
      '@hooks':      path.resolve(__dirname, 'src/hooks'),
      '@stores':     path.resolve(__dirname, 'src/stores'),
      '@types':      path.resolve(__dirname, 'src/types'),
      '@lib':        path.resolve(__dirname, 'src/lib'),
    },
  },
});
