import { defineConfig } from "vite";
import { resolve } from "node:path";

export default defineConfig({
  build: {
    lib: {
      entry: resolve(__dirname, "src/bundle.manifests.ts"),
      name: "lightning-ui",
      fileName: () => `lightning-ui.js`,
      formats: ["es"],
    },
    rollupOptions: {
      external: [/^@umbraco-cms\//],
    },
    sourcemap: true,
    outDir: resolve(__dirname, "../wwwroot/App_Plugins/Tollervey.Umbraco.LightningPayments"),
    emptyOutDir: false,
  },
});
