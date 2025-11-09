export const manifests: any = [
  // Paywall property editor UI
  {
    type: 'propertyEditorUi',
    alias: 'Our.Umbraco.Bitcoin.LightningPayments.PaywallUi',
    name: 'Lightning Paywall (UI)',
    elementName: 'our-paywall-editor',
    js: () => import('./paywall-editor.element'),
    meta: {
      label: 'Lightning Paywall',
      icon: 'icon-lock',
      group: 'common',
      propertyEditorSchemaAlias: 'Our.Umbraco.Bitcoin.LightningPayments.Paywall'
    }
  },
  // Paywall schema (the alias visible when creating Data Types)
  {
    type: 'propertyEditorSchema',
    alias: 'Our.Umbraco.Bitcoin.LightningPayments.Paywall',
    name: 'Lightning Paywall',
    meta: {
      defaultPropertyEditorUiAlias: 'Our.Umbraco.Bitcoin.LightningPayments.PaywallUi',
      settings: {
        properties: [],
        defaultData: [] as any
      }
    }
  },
  // Tip Jar property editor UI
  {
    type: 'propertyEditorUi',
    alias: 'Our.Umbraco.Bitcoin.LightningPayments.TipJarUi',
    name: 'Tip Jar (UI)',
    elementName: 'our-tipjar-editor',
    js: () => import('./tipjar-editor.element'),
    meta: {
      label: 'Tip Jar',
      icon: 'icon-coins',
      group: 'common',
      propertyEditorSchemaAlias: 'Our.Umbraco.Bitcoin.LightningPayments.TipJar'
    }
  },
  // Tip Jar schema
  {
    type: 'propertyEditorSchema',
    alias: 'Our.Umbraco.Bitcoin.LightningPayments.TipJar',
    name: 'Tip Jar',
    meta: {
      defaultPropertyEditorUiAlias: 'Our.Umbraco.Bitcoin.LightningPayments.TipJarUi',
      settings: {
        properties: [],
        defaultData: [] as any
      }
    }
  }
];