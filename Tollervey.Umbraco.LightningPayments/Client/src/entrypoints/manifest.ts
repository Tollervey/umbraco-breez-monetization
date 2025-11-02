export const manifests = [
    {
        type: 'entryPoint',
        alias: 'Tollervey.Umbraco.LightningPayments.Entrypoint',
        name: 'Lightning Payments Entrypoint',
        js: () => import('./entrypoint.js')
    }
];