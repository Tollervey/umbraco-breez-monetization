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
            views: [
                // Built-in Dashboard view (discovers dashboards bound to this workspace)
                { alias: 'Umb.WorkspaceView.Dashboard', name: 'Dashboard', weight: 0 },
                // Fallback view that mounts our element directly so we can see logs even if discovery fails
                { alias: 'Tollervey.LightningPayments.WorkspaceView.Main', name: 'Lightning UI', elementName: 'lightning-payments-dashboard', js: () => import('./dashboard.element'), weight: 1 }
            ]
        }
    },
    {
        type: 'dashboard',
        alias: 'Tollervey.LightningPayments.Dashboard',
        name: 'Lightning Payments Dashboard',
        element: 'lightning-payments-dashboard',
        loader: () => import('./dashboard.element'),
        weight: 10,
        meta: {
            label: 'Lightning Payments',
            pathname: 'lightning-payments',
            icon: 'icon-thunder'
        },
        // Bind this dashboard to the custom workspace so the Dashboard view can discover it
        conditions: [{ alias: 'Umb.Condition.WorkspaceAlias', value: 'Tollervey.LightningPayments.Workspace' }]
    }    
];