import path from 'path';
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => {
    // Load env from frontend directory
    const env = loadEnv(mode, __dirname, '');
    
    // Backend URL for proxy (without /api suffix)
    const backendUrl = env.VITE_BACKEND_URL || 'http://localhost:5000';
    
    console.log(`[Vite] Proxy /api -> ${backendUrl}`);
    
    return {
      server: {
        port: 5173,
        host: '0.0.0.0',
        proxy: {
          '/api': {
            target: backendUrl,
            changeOrigin: true,
            secure: false,
            configure: (proxy, _options) => {
              proxy.on('proxyReq', (proxyReq, req, _res) => {
                console.log(`[Proxy] ${req.method} ${req.url} -> ${backendUrl}${req.url}`);
              });
            },
          },
        },
      },
      plugins: [react()],
      resolve: {
        alias: {
          '@': path.resolve(__dirname, '.'),
          '@core': path.resolve(__dirname, './core'),
          '@features': path.resolve(__dirname, './features'),
          '@shared': path.resolve(__dirname, './shared'),
        }
      }
    };
});
