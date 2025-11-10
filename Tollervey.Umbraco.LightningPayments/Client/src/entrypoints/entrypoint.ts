import type {
  UmbEntryPointOnInit,
  UmbEntryPointOnUnload,
} from "@umbraco-cms/backoffice/extension-api";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import { client } from "../api/client.gen.js";
import { manifests as dashboardManifests } from '../dashboards/manifest';

// load up the manifests here
export const onInit: UmbEntryPointOnInit = (host, extensionRegistry) => {
    console.info('[LightningPayments] entrypoint onInit');
    console.info('[LightningPayments] Checking if manifests are already registered...'); // Added: Log to detect duplicates

    // Check for existing registrations to avoid duplicates
    const existing = extensionRegistry.extensionsOfType('section').filter(e => e.alias.startsWith('Tollervey.LightningPayments'));
    if (existing.length > 0) {
        console.warn('[LightningPayments] Manifests already registered, skipping:', existing.map(e => e.alias));
        return;
    }

    const toRegister = [...dashboardManifests];
    console.info('[LightningPayments] manifests to register:', toRegister.map(m => `${m.type}:${m.alias}`));
    toRegister.forEach((m) => extensionRegistry.register(m));
    console.info('[LightningPayments] registered manifests count:', toRegister.length);

    host.consumeContext(UMB_AUTH_CONTEXT, async (authContext) => {
      const config = authContext?.getOpenApiConfiguration();
      console.info('[LightningPayments] OpenAPI base:', config?.base);

      client.setConfig({
        auth: config?.token ?? undefined,
        baseUrl: config?.base ?? "",
        credentials: config?.credentials ?? "same-origin",
      });
    });
};

export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
  console.log('[LightningPayments] entrypoint onUnload');
};
