export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Our Umbraco Bitcoin Lightning Payments Entrypoint",
    alias: "Our.Umbraco.Bitcoin.LightningPayments.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];
