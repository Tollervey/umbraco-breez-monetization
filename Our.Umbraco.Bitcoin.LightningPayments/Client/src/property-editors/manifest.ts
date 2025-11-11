export const manifests: Array<any> = [
  // Paywall Message property editor UI
  {
    type: 'propertyEditorUi',
    alias: 'Our.Umbraco.Bitcoin.LightningPayments.PaywallMessageUi',
    name: 'Paywall Message (UI)',
    elementName: 'paywall-message-editor',
    js: () => import('./paywall-message-editor.element.js'),
    meta: {
      label: 'Paywall Message',
      icon: 'icon-edit',
      group: 'common',
      propertyEditorSchemaAlias: 'Our.Umbraco.Bitcoin.LightningPayments.PaywallMessage'
    }
  },
  // Paywall Message schema
  {
    type: 'propertyEditorSchema',
    alias: 'Our.Umbraco.Bitcoin.LightningPayments.PaywallMessage',
    name: 'Paywall Message',
    meta: {
      defaultPropertyEditorUiAlias: 'Our.Umbraco.Bitcoin.LightningPayments.PaywallMessageUi',
      valueType: 'TEXT',
      settings: {
        properties: [],
        defaultData: {}
      }
    }
  }
];