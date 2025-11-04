// Combined manifests from entrypoints, dashboards, and property-editors

// From entrypoints/manifest.ts
const entrypointManifests = [
  {
    type: 'backofficeEntryPoint',
    alias: 'Tollervey.Umbraco.LightningPayments.Entrypoint',
    name: 'Lightning Payments Entrypoint',
    js: () => import('./entrypoints/entrypoint') // Adjust path if needed; assumes entrypoint.ts is moved to src/entrypoints/
  }
];

// From dashboards/manifest.ts
const dashboardManifests = [
  {
    type: 'section',
    alias: 'Tollervey.LightningPayments.Section',
    name: 'Lightning Payments',
    weight: 10,
    meta: {
      label: 'Lightning Payments',
      pathname: 'lightning-payments',
      icon: 'icon-thunder',
      // ensure a workspace is selected when opening the section
      defaultWorkspace: 'Tollervey.LightningPayments.Workspace'
    }
  },
  {
    type: 'workspace',
    alias: 'Tollervey.LightningPayments.Workspace',
    name: 'Lightning Payments Workspace',
    conditions: [{ alias: 'Umb.Condition.SectionAlias', value: 'Tollervey.LightningPayments.Section' }],
    meta: {
      entityType: 'lightning',
      views: [
        { alias: 'Umb.WorkspaceView.Dashboard', name: 'Dashboard', weight: 0 },
        { alias: 'Tollervey.LightningPayments.WorkspaceView.Main', name: 'Lightning UI', weight: 1 }
      ]
    }
  },
  {
    // register our direct view that mounts the web component
    type: 'workspaceView',
    alias: 'Tollervey.LightningPayments.WorkspaceView.Main',
    name: 'Lightning UI',
    elementName: 'lightning-payments-dashboard',
    js: () => import('./dashboards/dashboard.element'), // Adjust path if needed; assumes dashboard.element.ts is moved to src/dashboards/
    conditions: [{ alias: 'Umb.Condition.WorkspaceAlias', value: 'Tollervey.LightningPayments.Workspace' }]
  },
  {
    // keep the dashboard so the built-in Dashboard view can also show it
    type: 'dashboard',
    alias: 'Tollervey.LightningPayments.Dashboard',
    name: 'Lightning Payments Dashboard',
    element: 'lightning-payments-dashboard',
    loader: () => import('./dashboards/dashboard.element'), // Adjust path if needed
    weight: 10,
    meta: {
      label: 'Lightning Payments',
      pathname: 'lightning-payments',
      icon: 'icon-thunder'
    },
    conditions: [{ alias: 'Umb.Condition.WorkspaceAlias', value: 'Tollervey.LightningPayments.Workspace' }]
  }
];

// From property-editors/manifest.ts
const propertyEditorManifests = [
  // Paywall property editor UI
  {
    type: 'propertyEditorUi',
    alias: 'Tollervey.Breez.PaywallUi',
    name: 'Lightning Paywall (UI)',
    elementName: 'tollervey-paywall-editor',
    js: () => import('./property-editors/paywall-editor.element'), // Adjust path if needed; assumes files are moved to src/property-editors/
    meta: {
      label: 'Lightning Paywall',
      icon: 'icon-lock',
      group: 'common',
      propertyEditorSchemaAlias: 'Tollervey.Breez.Paywall'
    }
  },
  // Paywall schema (the alias visible when creating Data Types)
  {
    type: 'propertyEditorSchema',
    alias: 'Tollervey.Breez.Paywall',
    name: 'Lightning Paywall',
    meta: {
      defaultPropertyEditorUiAlias: 'Tollervey.Breez.PaywallUi',
      // Value stored as JSON string
      valueType: 'JSON',
      settings: {
        properties: [],
        defaultData: {}
      }
    }
  },
  // Tip Jar property editor UI
  {
    type: 'propertyEditorUi',
    alias: 'Tollervey.Breez.TipJarUi',
    name: 'Tip Jar (UI)',
    elementName: 'tollervey-tipjar-editor',
    js: () => import('./property-editors/tipjar-editor.element'), // Adjust path if needed
    meta: {
      label: 'Tip Jar',
      icon: 'icon-coins',
      group: 'common',
      propertyEditorSchemaAlias: 'Tollervey.Breez.TipJar'
    }
  },
  // Tip Jar schema
  {
    type: 'propertyEditorSchema',
    alias: 'Tollervey.Breez.TipJar',
    name: 'Tip Jar',
    meta: {
      defaultPropertyEditorUiAlias: 'Tollervey.Breez.TipJarUi',
      valueType: 'JSON',
      settings: {
        properties: [],
        defaultData: { enabled: false, defaultAmounts: [500,1000,2500], label: 'Send a tip' }
      }
    }
  }
];

// Export combined manifests
export const manifests = [
  ...entrypointManifests,
  ...dashboardManifests,
  ...propertyEditorManifests,
];