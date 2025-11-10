export const manifests: Array<any> = [
 // Paywall property editor UI
 {
 type: 'propertyEditorUi',
 alias: 'Tollervey.Breez.PaywallUi',
 name: 'Lightning Paywall (UI)',
 elementName: 'tollervey-paywall-editor',
 js: () => import('./paywall-editor.element.js'),
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
 js: () => import('./tipjar-editor.element.js'),
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
