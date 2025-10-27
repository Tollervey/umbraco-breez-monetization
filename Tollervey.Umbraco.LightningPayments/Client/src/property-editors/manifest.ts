export const manifests: Array<any> = [
 {
 type: 'propertyEditorUi',
 alias: 'Tollervey.PaywallEditor',
 name: 'Lightning Paywall',
 elementName: 'tollervey-paywall-editor',
 js: () => import('./paywall-editor.element.js'),
 meta: {
 label: 'Lightning Paywall',
 icon: 'icon-lock',
 group: 'common'
 }
 }
];
