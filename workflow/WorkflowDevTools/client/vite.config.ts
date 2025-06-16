/// <reference types="vitest" />
import { defineConfig } from "vite"
import react from "@vitejs/plugin-react"

import * as path from "path"
// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  css: {
    modules: {
      localsConvention: "camelCase",
    },
  },
  build: {
    outDir: path.resolve(__dirname, "../wwwroot"),
    minify: false,
    emptyOutDir: true,
  },
  server: {
    port: 5173, // Default Vite port
    strictPort: true,
    proxy: {
      "/api": {
        target: "https://localhost:3000", // Your ASP.NET Core API port
        secure: false,
        changeOrigin: true,
      },
    },
  },
  test: {
    globals: true,
    environment: "jsdom",
    setupFiles: "./setup-test.ts",
  },
})
