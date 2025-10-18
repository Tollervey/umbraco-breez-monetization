const n = [
  {
    name: "My Extension Entrypoint",
    alias: "MyExtension.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint-BqzT7OLN.js")
  }
], t = [
  {
    type: "dashboard",
    alias: "lightning-payments-dashboard",
    name: "Lightning Payments Dashboard",
    elementName: "lightning-payments-dashboard",
    js: () => import("./dashboard.element-BzMSXEtf.js"),
    meta: {
      label: "Lightning Payments",
      pathname: "lightning-payments"
    }
  }
], a = [
  ...n,
  ...t
];
export {
  a as manifests
};
//# sourceMappingURL=my-extension.js.map
