import { defineConfig } from "vite";

export default defineConfig({
  build: {
    outDir: "../wwwroot/App_Plugins/MyExtension",
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      input: {
        "my-extension": "src/bundle.manifests.ts",
        "lightning-ui": "src/website/website.entry.ts",
      },
      external: [/^@umbraco/],
      output: {
        entryFileNames: (chunk) => `${chunk.name}.js`,
        chunkFileNames: (chunk) => `${chunk.name}.js`,
        assetFileNames: (asset) => `${asset.name}`,
        format: "es",
      },
    },
  },
});
