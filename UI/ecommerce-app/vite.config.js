import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';
// https://vitejs.dev/config/
export default defineConfig({
    plugins: [react()],
    resolve: {
        alias: {
            '@': path.resolve(__dirname, 'src'),
            '@api': path.resolve(__dirname, 'src/api'),
            '@components': path.resolve(__dirname, 'src/components'),
            '@features': path.resolve(__dirname, 'src/features'),
            '@hooks': path.resolve(__dirname, 'src/hooks'),
            '@stores': path.resolve(__dirname, 'src/stores'),
            '@types': path.resolve(__dirname, 'src/types'),
            '@lib': path.resolve(__dirname, 'src/lib'),
        },
    },
    build: {
        sourcemap: 'hidden',
    },
});
