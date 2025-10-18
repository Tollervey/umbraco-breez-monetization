export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'dashboard',
    alias: 'lightning-payments-dashboard',
    name: 'Lightning Payments Dashboard',
    elementName: 'lightning-payments-dashboard',
    js: () => import('./dashboard.element.js'),
    meta: {
      label: 'Lightning Payments',
      pathname: 'lightning-payments'
    }
  }
];
