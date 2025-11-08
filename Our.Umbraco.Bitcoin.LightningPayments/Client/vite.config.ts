import { defineConfig } from "vite";

export default defineConfig({
    build: {
        lib: {
            entry: {
                // Explicit entry for the dashboard web component, named to match your manifest's 'dashboard.element.js'
                // Adjust the source path to match your structure (src/entrypoints/entrypoint.ts likely imports/defines the component)
                "dashboard.element": "src/entrypoints/entrypoint.ts"
            },
            formats: ["es"],
        },
        outDir: "../wwwroot/App_Plugins/OurUmbracoBitcoinLightningPayments", // Keeps your current output location
        emptyOutDir: true,
        sourcemap: true,
        rollupOptions: {
            external: [/^@umbraco/], // Exclude Umbraco deps
            output: {
                // Disable hashes for entries, chunks, and assets
                entryFileNames: "[name].js",  // Outputs dashboard.element.js (unhashed)
                chunkFileNames: "[name].js",  // Any split chunks get unhashed names
                assetFileNames: "[name].[ext]"  // For CSS or other assets if present
            }
        }
    },
    base: "/App_Plugins/OurUmbracoBitcoinLightningPayments/"  // Ensures correct asset paths in the browser
});