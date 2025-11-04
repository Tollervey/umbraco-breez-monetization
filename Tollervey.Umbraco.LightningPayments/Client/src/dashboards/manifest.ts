export const manifests = [
  {
    type: 'section',
    alias: 'Tollervey.LightningPayments.Section',
    name: 'Lightning Payments',
    weight: 10,
    meta: {
      label: 'Lightning Payments',
      pathname: 'lightning-payments',
      icon: 'icon-thunder',
      // ensure a workspace is selected when opening the section
      defaultWorkspace: 'Tollervey.LightningPayments.Workspace'
    }
  },
  {
    type: 'workspace',
    alias: 'Tollervey.LightningPayments.Workspace',
    name: 'Lightning Payments Workspace',
    conditions: [{ alias: 'Umb.Condition.SectionAlias', value: 'Tollervey.LightningPayments.Section' }],
    meta: {
      entityType: 'lightning',
      views: [
        { alias: 'Umb.WorkspaceView.Dashboard', name: 'Dashboard', weight: 0 },
        { alias: 'Tollervey.LightningPayments.WorkspaceView.Main', name: 'Lightning UI', weight: 1 }
      ]
    }
  },
  {
    // register our direct view that mounts the web component
    type: 'workspaceView',
    alias: 'Tollervey.LightningPayments.WorkspaceView.Main',
    name: 'Lightning UI',
    elementName: 'lightning-payments-dashboard',
    js: () => import('./dashboard.element'),
    conditions: [{ alias: 'Umb.Condition.WorkspaceAlias', value: 'Tollervey.LightningPayments.Workspace' }]
  },
  {
    // keep the dashboard so the built-in Dashboard view can also show it
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