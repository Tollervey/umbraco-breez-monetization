import { defineConfig } from "vite";
import { resolve } from "node:path";

export default defineConfig({
  build: {
    lib: {
      entry: resolve(__dirname, "src/manifests.ts"), // Updated to new combined manifests file
      name: "lightning-ui",
      fileName: () => `lightning-ui.js`,
      formats: ["es"],
    },
    rollupOptions: {
      external: [/^@umbraco-cms\//],
      output: {
        entryFileNames: `lightning-ui.js`,
        chunkFileNames: `bundle.manifests.[hash].js`, // Add hash to prevent name conflicts
        assetFileNames: `[name][extname]`,
        manualChunks: (id) => {
          // Group all manifests, dashboards, entrypoints, and property-editors into one chunk
          if (id.includes('manifest') || id.includes('dashboard') || id.includes('entrypoint') || id.includes('property-editor')) {
            return 'bundle.manifests';
          }
        },
      },
    },
    sourcemap: true,
    chunkSizeWarningLimit: 1000, // Increase to allow larger chunks
    outDir: resolve(__dirname, "../wwwroot/App_Plugins/Tollervey.Umbraco.LightningPayments"),
    emptyOutDir: true, // Clean folder to avoid mismatches
  },
});
