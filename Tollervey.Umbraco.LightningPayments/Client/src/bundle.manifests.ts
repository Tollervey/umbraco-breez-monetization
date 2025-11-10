import { manifests as entrypoints } from "./entrypoints/manifest";
import { manifests as propertyEditors } from "./property-editors/manifest";
import { manifests as dashboards } from "./dashboards/manifest";

// We load this bundle from umbraco-package.json
export const manifests = [
 ...entrypoints,
 ...propertyEditors,
 ...dashboards,
];
