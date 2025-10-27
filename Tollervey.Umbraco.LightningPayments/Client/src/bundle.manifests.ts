import { manifests as propertyEditors } from "./property-editors/manifest.js";
import { manifests as dashboards } from "./dashboards/manifest.js";

// We load this bundle from umbraco-package.json
export const manifests = [
 ...propertyEditors,
 ...dashboards,
];
