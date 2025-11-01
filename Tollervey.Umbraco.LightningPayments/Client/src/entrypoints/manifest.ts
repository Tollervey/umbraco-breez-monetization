import type { ManifestDashboard } from '@umbraco-cms/backoffice/extension-registry';

export const manifests: Array<ManifestDashboard> = [
  {
    type: 'dashboard',
    alias: 'Tollervey.LightningPayments.Setup',
    name: 'Lightning Payments Setup',
    element: 'umb-lp-setup-dashboard',
    loader: () => import('../backoffice/lightning-setup-dashboard.element.js'),
    weight: 10,
    meta: {
      label: 'Lightning Payments Setup',
      pathname: 'lightning-payments-setup',
      icon: 'icon-thunder'
    },
    conditions: [
      { alias: 'Umb.Condition.SectionAlias', value: 'settings' }
    ]
  }
];
