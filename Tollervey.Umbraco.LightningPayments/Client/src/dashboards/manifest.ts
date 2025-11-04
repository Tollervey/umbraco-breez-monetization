export const manifests = [
  {
    type: 'section',
    alias: 'Tollervey.LightningPayments.Section',
    name: 'Lightning Payments',
    weight: 10,
    meta: { label: 'Lightning Payments', pathname: 'lightning-payments', icon: 'icon-thunder' }
  },
  {
    // Custom workspace for the section
    type: 'workspace',
    alias: 'Tollervey.LightningPayments.Workspace',
    name: 'Lightning Payments Workspace',
    conditions: [{ alias: 'Umb.Condition.SectionAlias', value: 'Tollervey.LightningPayments.Section' }],
    meta: {
      entityType: 'lightning',
      views: [
        // Built-in Dashboard host (discovers dashboards by WorkspaceAlias)
        { alias: 'Umb.WorkspaceView.Dashboard', name: 'Dashboard', weight: 0 },
        // Our own view that mounts the element directly (fallback/diagnostics)
        { alias: 'Tollervey.LightningPayments.WorkspaceView.Main', name: 'Lightning UI', weight: 1 }
      ]
    }
  },
  {
    // Register the custom workspace view that mounts our element
    type: 'workspaceView',
    alias: 'Tollervey.LightningPayments.WorkspaceView.Main',
    name: 'Lightning UI',
    elementName: 'lightning-payments-dashboard',
    js: () => import('./dashboard.element'),
    conditions: [{ alias: 'Umb.Condition.WorkspaceAlias', value: 'Tollervey.LightningPayments.Workspace' }]
  },
  {
    // Keep the dashboard for the built-in Dashboard view
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
    conditions: [{ alias: 'Umb.Condition.WorkspaceAlias', value: 'Tollervey.LightningPayments.Workspace' }]
  }
];