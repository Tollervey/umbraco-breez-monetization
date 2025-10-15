export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "umbracobreezmonetizationEntrypoint",
    alias: "umbraco_breez_monetization.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];
