# Lightning Payments UI (Website components)

This folder contains the front-end web components and helpers for the Umbraco Lightning Payments package.

The components are framework-agnostic custom elements built with Lit. You can use them in any web app or Umbraco template once the bundle is loaded.

- Entry point: `src/website/website.entry.ts`
- Distributed script (when built): `wwwroot/App_Plugins/Tollervey.Umbraco.LightningPayments/lightning-ui.js`

## Quick start (Umbraco view)

1) Build the client bundle

- From `Client/` run: `npm install` then `npm run build` (or `npm run watch`).

2) Reference the bundle in your Umbraco view/layout

```html
<script type="module" src="/App_Plugins/Tollervey.Umbraco.LightningPayments/lightning-ui.js"></script>
<link rel="stylesheet" href="/App_Plugins/Tollervey.Umbraco.LightningPayments/lightning-ui.css" />
```

3) Drop in a component

```html
<breez-paywall content-id="123"></breez-paywall>
```

---

## Components overview

### <breez-paywall>
Pay to unlock a content item by `content-id`. Handles invoice generation, QR display, polling, and real?time success events.

Attributes/props:
- `content-id` (number) – Required. The Umbraco content id.
- `title`, `description` – Text shown in the modal.
- `button-label` – Open button label.
- `enableLnurl` (boolean) – Show LNURL?P option in the modal.
- `enableBolt12` (boolean) – Show BOLT12 offer option in the modal.

Events:
- `breez-unlocked` – Fired when payment is confirmed and content is unlocked.

Example:
```html
<breez-paywall
 content-id="123"
 title="Unlock content"
 description="One?time Lightning payment to access this article."
></breez-paywall>
```

Accessibility/i18n:
- All status messages use proper `role` and `aria-live`.
- Many labels are exposed as attributes (e.g. `checking-label`, `waiting-label`, `failed-label`, `expired-label`, `refresh-label`).

Realtime + manual refresh:
- Uses SSE subscription; shows a fallback “Refresh status” button while pending.

### <breez-tip-jar>
Creates a tip invoice and reuses the payment modal. Also surfaces simple stats.

Attributes/props:
- `amounts` (array) + `default-amount`
- `content-id` (optional)
- Labels: `tip-button-label`, `please-wait-label`, `thanks-label`, `stats-error-label`, `refresh-label`, `custom-amount-label`, etc.

Example:
```html
<breez-tip-jar
 default-amount="1000"
 amounts='[500,1000,2500,5000]'
 title="Send a tip"
 description="Thank you for supporting!"
></breez-tip-jar>
```

### <breez-payment-modal>
Low-level modal that displays a BOLT11 invoice by default and optionally offers WebLN, LNURL?P, and BOLT12.

Important props:
- `contentId` – Required for paywall flow (to fetch invoice/fees, LNURL, BOLT12).
- `invoice`, `paymentHash` – If you already created an invoice (e.g., tip flow) pass it here.
- `currency` – Fiat code (e.g., `USD`) for fiat approximation.
- Feature flags: `enable-lnurl`, `enable-bolt12`, and optional `lightning-address`.
- Label props for i18n: `generate-label`, `generating-label`, `regenerate-label`, `close-label`, `expires-in-label`, `expired-label`, `fees-label`, `total-label`, etc.

Example (pre-supplied invoice):
```html
<breez-payment-modal
 open
 title="Pay invoice"
 description="Scan or pay in wallet"
 invoice="<bolt11>"
 paymentHash="<hash>"
 enable-lnurl
 enable-bolt12
 lightning-address="pay@yourdomain.com"
></breez-payment-modal>
```

---

## Feature examples

### Fee quote (receive-side)
Anonymous public endpoint returns an estimate for receiving the configured paywall amount.

```ts
async function loadFeeQuote(contentId: number) {
 const res = await fetch(`/api/public/lightning/GetPaywallReceiveFeeQuote?contentId=${contentId}`);
 if (!res.ok) throw new Error(`HTTP ${res.status}`);
 const { amountSat, feesSat, method } = await res.json(); // method: bolt11 | bolt12
 console.log('Amount:', amountSat, 'Fees:', feesSat, 'Method:', method);
}
```

Use the modal’s built-in quote by simply opening it with `contentId`; it queries and renders the totals for you.

### Fiat approximation
Use the bundle’s CoinGecko helper directly, or rely on the modal.

```ts
import { CoinGeckoExchangeRateService } from '/App_Plugins/.../lightning-ui.js';

const svc = new CoinGeckoExchangeRateService();
const price = await svc.getBtcPrice('USD');
const dollars = svc.toFiat(21_000, price); // sats -> USD
```

In the modal, set `currency="EUR"` (default is `USD`).

### WebLN
If a browser wallet injects `window.webln`, the modal shows “Pay with WebLN”. Errors are handled inline and the QR remains visible. No extra config is needed.

### LNURL?P
Enable the LNURL?P option:

```html
<breez-paywall content-id="123" title="Unlock" description="..." ></breez-paywall>
<!-- internally opens modal with LNURL if enabled -->
```

On the low-level modal:

```html
<breez-payment-modal
 open
 contentId="123"
 enable-lnurl
 lightning-address="pay@yourdomain.com"
></breez-payment-modal>
```

The modal calls `/api/public/lightning/GetLnurlPayInfo?contentId=...` and renders a QR from the returned `lnurl` (bech32) or fallback `callback` URL. If you provide a Lightning Address it will be displayed with a copy helper.

### BOLT12 offer
Enable the BOLT12 option to fetch and display an offer from `/api/public/lightning/GetBolt12Offer?contentId=...`:

```html
<breez-payment-modal open contentId="123" enable-bolt12></breez-payment-modal>
```

The modal keeps the BOLT11 view as the default and toggles to the BOLT12 pane on demand.

---

## Theming
The components consume CSS custom properties (see `src/website/themes/theme-base.ts`). You can set them at `:root` or any scope.

Minimal example:
```css
:root {
 --lp-color-primary: #f89c1c;
 --lp-color-primary-hover: #e68a0a;
 --lp-color-surface: #fff;
 --lp-color-text: #222;
 --lp-color-text-muted: #666;
 --lp-color-bg: #fff;
 --lp-color-danger: #b00020;
 --lp-border:1px solid #e5e5e5;
 --lp-radius:8px;
 --lp-overlay: rgba(0,0,0,0.5);
 --lp-shadow:010px30px rgba(0,0,0,0.15);
}
```

You can also import `theme-base.ts` and compose additional tokens.

---

## Accessibility and i18n
- Status messages use `role="status"` with `aria-live="polite"`; errors use `role="alert"` with `aria-live="assertive"`.
- All visible strings are attributes on elements. Override them for localization (e.g., `generate-label`, `checking-label`, `waiting-label`, etc.).

---

## Real-time robustness
- Components connect to `/api/public/lightning/realtime/subscribe` via SSE.
- When SSE drops, the UI shows an offline hint and keeps polling with manual “Refresh status”.

---

## API surface (public endpoints)
- `GET /api/public/lightning/GetPaywallInvoice?contentId=` – Create BOLT11 invoice (paywall)
- `GET /api/public/lightning/GetPaywallReceiveFeeQuote?contentId=&bolt12=` – Receive fee quote
- `GET /api/public/lightning/GetLnurlPayInfo?contentId=` – LNURL?P metadata
- `GET /api/public/lightning/GetLnurlInvoice?amount=&contentId=` – LNURL callback (wallets)
- `GET /api/public/lightning/GetBolt12Offer?contentId=` – BOLT12 offer
- `POST /api/public/lightning/CreateTipInvoice` – Tip jar invoice
- `GET /api/public/lightning/GetPaymentStatus?contentId=` – Paywall session status
- `GET /api/public/lightning/GetPaymentStatusByHash?paymentHash=` – Status by payment hash

These are anonymous endpoints intended for front-end integrations.

---

## Bundling/consuming from code
If you consume via JS modules (e.g., Vite), import from the entry file:

```ts
import {
 CoinGeckoExchangeRateService,
 BreezPaymentModalBasic,
} from '/App_Plugins/Tollervey.Umbraco.LightningPayments/lightning-ui.js';
```

Or import from source when working inside this repo:

```ts
export * from './src/website/website.entry.ts';
```

---

## Troubleshooting
- If you don’t see the UI updates, ensure SSE endpoint `/api/public/lightning/realtime/subscribe` is reachable and not blocked by proxies.
- For LNURL?P, verify your site is served via HTTPS and callback URL is publicly reachable.
- For BOLT12, ensure your Breez SDK connection is healthy and supports offers.
