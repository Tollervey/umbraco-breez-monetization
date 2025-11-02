export const manifests = [
    {
        type: 'dashboard',
        alias: 'Tollervey.LightningPayments.Dashboard',
        name: 'Lightning Payments Dashboard',
        element: 'lightning-payments-dashboard',
        loader: () => import('./dashboard.element.js'),
        weight: 10,
        meta: {
            label: 'Lightning Payments',
            pathname: 'lightning-payments',
            icon: 'icon-thunder'
        },
        conditions: [{ alias: 'Umb.Condition.SectionAlias', value: 'settings' }]
    }
];