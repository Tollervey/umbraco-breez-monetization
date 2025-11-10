import type {
  UmbEntryPointOnInit,
  UmbEntryPointOnUnload,
} from "@umbraco-cms/backoffice/extension-api";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import { client } from "../api/client.gen.js";
import { manifests as propertyEditors } from "../property-editors/manifest.js";

// load up the manifests here
export const onInit: UmbEntryPointOnInit = (_host, _extensionRegistry) => {
  console.log("Hello from my extension 🎉");
  console.log("Entrypoint onInit called");
  // Register property editors
  _extensionRegistry.registerMany(propertyEditors);
  console.log("Property editors registered");
  // Will use only to add in Open API config with generated TS OpenAPI HTTPS Client
  // Do the OAuth token handshake stuff
  _host.consumeContext(UMB_AUTH_CONTEXT as any, async (authContext: any) => {
    console.log("Auth context consumed");
    // Get the token info from Umbraco
    const config = authContext?.getOpenApiConfiguration();
    console.log("OpenAPI config:", config);

    client.setConfig({
      auth: config?.token ?? undefined,
      baseUrl: config?.base ?? "",
      credentials: config?.credentials ?? "same-origin",
    });
    console.log("Client config set");
  });
};

export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
  console.log("Goodbye from my extension 👋");
};
