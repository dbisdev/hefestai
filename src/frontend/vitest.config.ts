/**
 * Vitest Configuration
 * Test configuration for React frontend with jsdom environment
 */
import path from 'path';
import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  test: {
    // Use jsdom for DOM testing
    environment: 'jsdom',
    
    // Setup files run before each test file
    setupFiles: ['./test/setup.ts'],
    
    // Global test utilities (describe, it, expect)
    globals: true,
    
    // Include test files pattern
    include: ['**/*.{test,spec}.{ts,tsx}'],
    
    // Exclude node_modules and dist
    exclude: ['node_modules', 'dist'],
    
    // Coverage configuration
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      exclude: [
        'node_modules/',
        'test/',
        '**/*.d.ts',
        '**/*.test.{ts,tsx}',
        '**/*.spec.{ts,tsx}',
      ],
    },
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, '.'),
      '@core': path.resolve(__dirname, './core'),
      '@features': path.resolve(__dirname, './features'),
      '@shared': path.resolve(__dirname, './shared'),
    },
  },
});
