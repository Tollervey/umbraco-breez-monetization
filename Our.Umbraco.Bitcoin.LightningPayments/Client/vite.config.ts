import { defineConfig } from "vite";

export default defineConfig({
    build: {
        lib: {
            entry: {
                // Build the dashboard web component from the correct source file
                "dashboard.element": "src/dashboards/dashboard.element.ts",
                // Build the website element
                "simple-poc-element": "src/elements/simple-poc-element.element.ts"
            },
            formats: ["es"],
        },
        outDir: "../wwwroot/App_Plugins/OurUmbracoBitcoinLightningPayments",
        emptyOutDir: true,
        sourcemap: true,
        rollupOptions: {
            external: [/^@umbraco/],
            output: {
                entryFileNames: "[name].js",
                chunkFileNames: "[name].js",
                assetFileNames: "[name].[ext]"
            }
        }
    },
    base: "/App_Plugins/OurUmbracoBitcoinLightningPayments/"
});