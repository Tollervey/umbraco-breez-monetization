export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "umbracobreezmonetizationDashboard",
    alias: "umbraco_breez_monetization.Dashboard",
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
