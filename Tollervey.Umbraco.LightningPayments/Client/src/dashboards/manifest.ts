export const manifests = [
    {
        type: 'section',
        alias: 'Tollervey.LightningPayments.Section',
        name: 'Lightning Payments',
        weight: 10,
        meta: { label: 'Lightning Payments', pathname: 'lightning-payments', icon: 'icon-thunder' }
    },
    {
        // Host a dashboard-style workspace for the custom section
        type: 'workspace',
        alias: 'Tollervey.LightningPayments.Workspace',
        name: 'Lightning Payments Workspace',
        conditions: [{ alias: 'Umb.Condition.SectionAlias', value: 'Tollervey.LightningPayments.Section' }],
        meta: {
            // entityType is arbitrary for a pure dashboard workspace
            entityType: 'lightning',
            views: [{ alias: 'Umb.WorkspaceView.Dashboard', name: 'Dashboard', weight: 0 }]
        }
    },
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
        conditions: [{ alias: 'Umb.Condition.SectionAlias', value: 'Tollervey.LightningPayments.Section' }]
    }    
];