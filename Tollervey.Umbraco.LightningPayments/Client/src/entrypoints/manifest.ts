export const manifests = [
  {
    type: 'backofficeEntryPoint',
    alias: 'Tollervey.Umbraco.LightningPayments.Entrypoint',
    name: 'Lightning Payments Entrypoint',
    js: () => import('./entrypoint.js')
  }
];