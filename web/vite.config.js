import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5512',
        changeOrigin: true
      },
      '/auth': {
        target: 'http://localhost:5512',
        changeOrigin: true
      }
    }
  }
})
