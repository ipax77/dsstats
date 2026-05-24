import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    // some paths to your test files
    include: ['Client/tests/**/*.test.ts'],
    // setup files
    setupFiles: ['Client/tests/setup.ts'],
    // test environment
    environment: 'happy-dom', // or 'jsdom', 'node'
    watch: false,
  },
  resolve: {
    alias: {
      './pako/index.js': 'pako',
    },
  },
});
