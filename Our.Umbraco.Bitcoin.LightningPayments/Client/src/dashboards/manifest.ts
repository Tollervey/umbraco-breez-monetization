export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Our Umbraco Bitcoin Lightning Payments Dashboard",
    alias: "Our.Umbraco.Bitcoin.LightningPayments.Dashboard",
    type: "dashboard",
    js: () => import("./dashboard.element.js"),
    meta: {
      label: "Example Dashboard",
      pathname: "example-dashboard",
    },
    conditions: [
      {
        alias: "Umb.Condition.SectionAlias",
        match: "Umb.Section.Content",
      },
    ],
  },
];
