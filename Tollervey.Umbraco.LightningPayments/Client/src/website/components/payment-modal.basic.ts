import { LitElement, html, css, nothing } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import './qr-code-display.js';
import './invoice-display.js';
import { CoinGeckoExchangeRateService, type FiatCode } from '../services/coingecko-exchange-rate-service.js';

@customElement('breez-payment-modal')
export class BreezPaymentModalBasic extends LitElement {
 // State/inputs
 @property({ type: Boolean, reflect: true }) open = false;
 @property({ type: Number }) amount =0;
 @property({ type: String }) title = 'Lightning Payment';
 @property({ type: String }) description = '';
 @property({ type: Number }) contentId?: number;
 // allow passing a pre-created invoice and hash (used by tip-jar)
 @property({ type: String }) invoice?: string;
 @property({ type: String }) paymentHash?: string;
 // Optional expiry ISO string from backend
 @property({ type: String }) expiry?: string;
 // Fiat currency for approximation
 @property({ type: String }) currency: FiatCode = 'USD';

 // Feature flags / optional inputs
 @property({ type: Boolean, attribute: 'enable-lnurl' }) enableLnurl = false;
 @property({ type: String, attribute: 'lightning-address' }) lightningAddress?: string;
 @property({ type: Boolean, attribute: 'enable-bolt12' }) enableBolt12 = false;

 // Localizable/Configurable labels
 @property({ type: String, attribute: 'generate-label' }) generateLabel = 'Generate Invoice';
 @property({ type: String, attribute: 'generating-label' }) generatingLabel = 'Generating…';
 @property({ type: String, attribute: 'regenerate-label' }) regenerateLabel = 'Generate new invoice';
 @property({ type: String, attribute: 'close-label' }) closeLabel = 'Close';
 @property({ type: String, attribute: 'missing-content-label' }) missingContentLabel = 'Missing or invalid content id';
 @property({ type: String, attribute: 'create-error-label' }) createErrorLabel = 'Failed to create invoice';
 @property({ type: String, attribute: 'expires-in-label' }) expiresInLabel = 'Expires in';
 @property({ type: String, attribute: 'expired-label' }) expiredLabel = 'Invoice expired. Generate a new one.';
 @property({ type: String, attribute: 'fees-label' }) feesLabel = 'Estimated receive fee';
 @property({ type: String, attribute: 'total-label' }) totalLabel = 'Estimated total';
 @property({ type: String, attribute: 'estimating-fees-label' }) estimatingFeesLabel = 'Estimating fees…';
 @property({ type: String, attribute: 'rates-loading-label' }) ratesLoadingLabel = 'Fetching rates…';
 @property({ type: String, attribute: 'rates-error-label' }) ratesErrorLabel = 'Failed to fetch rates';
 @property({ type: String, attribute: 'rates-retry-label' }) ratesRetryLabel = 'Retry';
 @property({ type: String, attribute: 'show-details-label' }) showDetailsLabel = 'Show details';
 @property({ type: String, attribute: 'hide-details-label' }) hideDetailsLabel = 'Hide details';
 @property({ type: String, attribute: 'copy-invoice-label' }) copyInvoiceLabel = 'Copy invoice';
 @property({ type: String, attribute: 'copied-label' }) copiedLabel = 'Copied';
 @property({ type: String, attribute: 'open-wallet-label' }) openWalletLabel = 'Open in wallet';
 @property({ type: String, attribute: 'webln-label' }) weblnLabel = 'Pay with WebLN';
 @property({ type: String, attribute: 'webln-error-label' }) weblnErrorLabel = 'WebLN payment failed.';
 @property({ type: String, attribute: 'lnurl-label' }) lnurlLabel = 'Pay via LNURL-P';
 @property({ type: String, attribute: 'lnurl-loading-label' }) lnurlLoadingLabel = 'Preparing LNURL…';
 @property({ type: String, attribute: 'lnurl-error-label' }) lnurlErrorLabel = 'Failed to prepare LNURL.';
 @property({ type: String, attribute: 'lightning-address-label' }) lightningAddressLabel = 'Lightning Address';
 @property({ type: String, attribute: 'copy-address-label' }) copyAddressLabel = 'Copy address';
 @property({ type: String, attribute: 'bolt12-label' }) bolt12Label = 'Try BOLT12';
 @property({ type: String, attribute: 'bolt12-loading-label' }) bolt12LoadingLabel = 'Preparing BOLT12…';
 @property({ type: String, attribute: 'bolt12-error-label' }) bolt12ErrorLabel = 'Failed to prepare BOLT12.';
 @property({ type: String, attribute: 'back-to-invoice-label' }) backToInvoiceLabel = 'Back to invoice';

 @state() private _invoice?: { bolt11: string; paymentHash: string };
 @state() private _loading = false;
 @state() private _error = '';
 @state() private _remaining = '';
 @state() private _isExpired = false;
 private _countdownTimer: number | null = null;

 // fee quote state (optional; best-effort)
 @state() private _feeLoading = false;
 @state() private _feeAmount: number | null = null; // amountSat echoed from API
 @state() private _feeSat: number | null = null; // feesSat from API
 @state() private _feeMethod: string | null = null; // bolt11/bolt12 indicator

 // fiat conversion state
 private _rates = new CoinGeckoExchangeRateService();
 @state() private _fiatLoading = false;
 @state() private _fiatError = '';
 @state() private _fiatTotal: number | null = null;

 // details panel state
 @state() private _detailsOpen = false;
 @state() private _copyOk = false;
 private _copyTimer: number | null = null;

 // WebLN state
 @state() private _weblnAvailable = false;
 @state() private _weblnBusy = false;
 @state() private _weblnError = '';

 // LNURL state
 @state() private _lnurlOpen = false;
 @state() private _lnurlLoading = false;
 @state() private _lnurlError = '';
 @state() private _lnurlValue: string | null = null; // bech32 or fallback URL
 @state() private _addrCopyOk = false;

 // BOLT12 state
 @state() private _bolt12Open = false;
 @state() private _bolt12Loading = false;
 @state() private _bolt12Error = '';
 @state() private _bolt12Offer: string | null = null;

 private _previouslyFocused: Element | null = null;
 private _titleId = `bpm-title-${Math.random().toString(36).slice(2)}`;
 private _descId = `bpm-desc-${Math.random().toString(36).slice(2)}`;

 private _sr(): ShadowRoot | null { return (this as any).shadowRoot ?? null; }

 private _emitClose() {
 (this as unknown as HTMLElement).dispatchEvent(new CustomEvent('close', { bubbles: true, composed: true }));
 }

 private _handleClose = () => {
 this.open = false;
 this._emitClose();
 };

 private _checkWebln() { this._weblnAvailable = !!(window as any)?.webln; }

 private _onKeyDown = (e: KeyboardEvent) => {
 if (!this.open) return;
 if (e.key === 'Escape') {
 e.preventDefault();
 this._handleClose();
 return;
 }
 if (e.key === 'Tab') {
 // Focus trap inside modal
 const focusables = this._getFocusable();
 if (focusables.length ===0) return;
 const first = focusables[0];
 const last = focusables[focusables.length -1];
 const active = ((this as any).shadowRoot?.activeElement as HTMLElement | null) ?? (document.activeElement as HTMLElement | null);
 if (e.shiftKey) {
 if (!active || active === first) {
 e.preventDefault();
 (last as HTMLElement).focus();
 }
 } else {
 if (!active || active === last) {
 e.preventDefault();
 (first as HTMLElement).focus();
 }
 }
 }
 };

 private _getFocusable(): HTMLElement[] {
 const container = this._sr()?.querySelector('.modal') as HTMLElement | null;
 if (!container) return [];
 const selectors = [
 'a[href]', 'button:not([disabled])', 'textarea:not([disabled])', 'input:not([disabled])',
 'select:not([disabled])', '[tabindex]:not([tabindex="-1"])'
 ];
 return Array.from(container.querySelectorAll<HTMLElement>(selectors.join(',')))
 .filter(el => !el.hasAttribute('inert') && el.offsetParent !== null);
 }

 private _focusInitial() {
 // Focus the modal container or first focusable
 const modal = this._sr()?.querySelector('.modal') as HTMLElement | null;
 const focusables = this._getFocusable();
 if (focusables.length) focusables[0].focus();
 else modal?.focus();
 }

 private _lockScroll(lock: boolean) {
 if (lock) {
 document.documentElement.style.overflow = 'hidden';
 document.body.style.overflow = 'hidden';
 } else {
 document.documentElement.style.overflow = '';
 document.body.style.overflow = '';
 }
 }

 private _updateCountdown() {
 if (!this.expiry) {
 this._remaining = '';
 this._isExpired = false;
 return;
 }
 const expiryTs = Date.parse(this.expiry);
 if (isNaN(expiryTs)) {
 this._remaining = '';
 this._isExpired = false;
 return;
 }
 const now = Date.now();
 const ms = expiryTs - now;
 if (ms <=0) {
 this._remaining = this.expiredLabel;
 this._isExpired = true;
 this._clearCountdown();
 return;
 }
 const totalSec = Math.floor(ms /1000);
 const m = Math.floor(totalSec /60);
 const s = totalSec %60;
 const s2 = s <10 ? `0${s}` : String(s);
 this._remaining = `${this.expiresInLabel} ${m}:${s2}`;
 this._isExpired = false;
 }

 private _startCountdown() {
 this._clearCountdown();
 this._isExpired = false;
 this._updateCountdown();
 this._countdownTimer = window.setInterval(() => this._updateCountdown(),1000);
 }

 private _clearCountdown() {
 if (this._countdownTimer) {
 clearInterval(this._countdownTimer);
 this._countdownTimer = null;
 }
 }

 private _resetInvoiceState() {
 this._invoice = undefined;
 this.expiry = undefined;
 this._remaining = '';
 this._isExpired = false;
 this._detailsOpen = false;
 this._copyOk = false;
 if (this._copyTimer) { clearTimeout(this._copyTimer); this._copyTimer = null; }
 // reset alt modes
 this._lnurlOpen = false; this._lnurlError = ''; this._lnurlValue = null; this._lnurlLoading = false;
 this._bolt12Open = false; this._bolt12Error = ''; this._bolt12Offer = null; this._bolt12Loading = false;
 }

 private async _regenerateInvoice() {
 if (this._loading) return;
 this._resetInvoiceState();
 await this._generateInvoice();
 }

 private async _loadFeeQuote() {
 if (!this.contentId || this.contentId <=0) return;
 this._feeLoading = true;
 this._feeAmount = null;
 this._feeSat = null;
 this._feeMethod = null;
 try {
 const res = await fetch(`/api/public/lightning/GetPaywallReceiveFeeQuote?contentId=${this.contentId}`);
 if (!res.ok) throw new Error(`HTTP ${res.status}`);
 const data = await res.json();
 const amt = Number(data?.amountSat);
 const fees = Number(data?.feesSat);
 this._feeAmount = Number.isFinite(amt) ? amt : null;
 this._feeSat = Number.isFinite(fees) ? fees : null;
 this._feeMethod = typeof data?.method === 'string' ? data.method : null;
 } catch {
 // best-effort: ignore errors for fee quote
 } finally {
 this._feeLoading = false;
 this._updateFiatIfReady();
 }
 }

 private _totalSats(): number | null {
 if (this._feeAmount != null) {
 const fees = this._feeSat ??0;
 return this._feeAmount + fees;
 }
 return null;
 }

 private async _loadFiat() {
 const total = this._totalSats();
 if (total == null) { this._fiatTotal = null; return; }
 this._fiatLoading = true;
 this._fiatError = '';
 try {
 const price = await this._rates.getBtcPrice(this.currency);
 this._fiatTotal = this._rates.toFiat(total, price);
 } catch (e: any) {
 this._fiatError = e?.message ?? 'rate failed';
 this._fiatTotal = null;
 } finally {
 this._fiatLoading = false;
 }
 }

 private _updateFiatIfReady() {
 const total = this._totalSats();
 if (total != null) this._loadFiat();
 }

 private _retryRates = () => this._loadFiat();
 private _toggleDetails = () => { this._detailsOpen = !this._detailsOpen; };
 private async _copyInvoice() {
 if (!this._invoice?.bolt11) return;
 try {
 await navigator.clipboard.writeText(this._invoice.bolt11);
 this._copyOk = true;
 if (this._copyTimer) clearTimeout(this._copyTimer);
 this._copyTimer = window.setTimeout(() => { this._copyOk = false; },1500);
 } catch { /* ignore */ }
 }
 private async _copyAddress() {
 if (!this.lightningAddress) return;
 try { await navigator.clipboard.writeText(this.lightningAddress); this._addrCopyOk = true; setTimeout(() => this._addrCopyOk = false,1500); } catch {}
 }

 private _checkWeblnOnOpenOrInvoice() { this._checkWebln(); }
 private _payWithWebln = async () => {
 const inv = this._invoice?.bolt11;
 const webln: any = (window as any).webln;
 if (!inv || !webln) return;
 this._weblnBusy = true; this._weblnError = '';
 try {
 if (typeof webln.enable === 'function') { await webln.enable(); }
 await webln.sendPayment(inv);
 } catch (e: any) {
 this._weblnError = e?.message ?? this.weblnErrorLabel;
 } finally {
 this._weblnBusy = false;
 }
 };

 private _toggleLnurl = async () => {
 if (!this.enableLnurl) return;
 this._lnurlOpen = !this._lnurlOpen;
 if (this._lnurlOpen && !this._lnurlValue && !this._lnurlLoading) {
 if (!this.contentId || this.contentId <=0) { this._lnurlError = this.missingContentLabel; return; }
 this._lnurlLoading = true; this._lnurlError = '';
 try {
 const res = await fetch(`/api/public/lightning/GetLnurlPayInfo?contentId=${this.contentId}`);
 if (!res.ok) throw new Error(`HTTP ${res.status}`);
 const data = await res.json();
 // Prefer bech32 lnurl if present, else fall back to callback url
 const bech32 = typeof data?.lnurl === 'string' ? data.lnurl : null;
 const cb = typeof data?.callback === 'string' ? data.callback : null;
 this._lnurlValue = bech32 || cb;
 } catch (e: any) {
 this._lnurlError = e?.message ?? this.lnurlErrorLabel;
 } finally {
 this._lnurlLoading = false;
 }
 }
 };

 private _toggleBolt12 = async () => {
 if (!this.enableBolt12) return;
 // toggle
 const next = !this._bolt12Open;
 this._bolt12Open = next;
 if (next && !this._bolt12Offer && !this._bolt12Loading) {
 if (!this.contentId || this.contentId <=0) { this._bolt12Error = this.missingContentLabel; return; }
 this._bolt12Loading = true; this._bolt12Error = '';
 try {
 const res = await fetch(`/api/public/lightning/GetBolt12Offer?contentId=${this.contentId}`);
 if (!res.ok) throw new Error(`HTTP ${res.status}`);
 const offer = await res.text();
 this._bolt12Offer = offer?.trim() || null;
 } catch (e: any) {
 this._bolt12Error = e?.message ?? this.bolt12ErrorLabel;
 this._bolt12Open = false;
 } finally { this._bolt12Loading = false; }
 }
 };

 async _generateInvoice() {
 if (!this.contentId || this.contentId <=0) {
 this._error = this.missingContentLabel;
 return;
 }
 this._loading = true;
 this._error = '';
 try {
 const res = await fetch(`/api/public/lightning/GetPaywallInvoice?contentId=${this.contentId}`);
 if (!res.ok) throw new Error(`HTTP ${res.status}`);
 const data = await res.json();
 this._invoice = { bolt11: data.invoice, paymentHash: data.paymentHash };
 this.expiry = data.expiry ?? undefined;
 if (this.expiry) this._startCountdown();
 (this as unknown as HTMLElement).dispatchEvent(new CustomEvent('invoice-generated', { detail: { paymentHash: data.paymentHash }, bubbles: true, composed: true }));
 this._checkWeblnOnOpenOrInvoice();
 } catch (err: any) {
 this._error = err?.message ?? this.createErrorLabel;
 } finally {
 this._loading = false;
 }
 }

 updated(changed: Map<string, unknown>) {
 if (changed.has('open')) {
 if (this.open) {
 const root: any = (this as any).getRootNode ? (this as any).getRootNode() : document;
 this._previouslyFocused = (root.activeElement as Element | null) ?? null;
 this._lockScroll(true);
 // Reset transient state when opening, unless a pre-supplied invoice is provided
 if (!this.invoice) {
 this._resetInvoiceState();
 }
 this._error = '';
 // Defer to ensure DOM is rendered
 setTimeout(() => this._focusInitial(),0);
 (this as any).addEventListener('keydown', this._onKeyDown);
 if (this.expiry) this._startCountdown();
 // Kick off best-effort fee quote for paywall flows (no pre-supplied invoice)
 if (!this.invoice) this._loadFeeQuote();
 this._checkWebln();
 } else {
 this._lockScroll(false);
 (this as any).removeEventListener('keydown', this._onKeyDown);
 this._clearCountdown();
 if (this._previouslyFocused && (this._previouslyFocused as HTMLElement).focus) {
 (this._previouslyFocused as HTMLElement).focus();
 }
 }
 }

 if (changed.has('open') || changed.has('invoice') || changed.has('paymentHash')) {
 if (this.open && this.invoice && this.paymentHash && !this._invoice) {
 this._invoice = { bolt11: this.invoice, paymentHash: this.paymentHash };
 (this as unknown as HTMLElement).dispatchEvent(new CustomEvent('invoice-generated', { detail: { paymentHash: this.paymentHash }, bubbles: true, composed: true }));
 this._checkWeblnOnOpenOrInvoice();
 }
 }

 if (changed.has('currency') || changed.has('_feeAmount') || changed.has('_feeSat')) {
 this._updateFiatIfReady();
 }
 }

 disconnectedCallback(): void {
 super.disconnectedCallback();
 // Safety: ensure no locks or listeners remain if element is removed while open
 (this as any).removeEventListener('keydown', this._onKeyDown);
 this._clearCountdown();
 this._lockScroll(false);
 if (this._copyTimer) { clearTimeout(this._copyTimer); this._copyTimer = null; }
 }

 private _renderFiat() {
 if (this._fiatLoading) return html`<div class="fiat" role="status" aria-live="polite">${this.ratesLoadingLabel}</div>`;
 if (this._fiatError) return html`<div class="fiat error">${this.ratesErrorLabel} <button class="link" @click=${this._retryRates}>${this.ratesRetryLabel}</button></div>`;
 if (this._fiatTotal != null) {
 const code = this.currency;
 const formatted = new Intl.NumberFormat(undefined, { style: 'currency', currency: code }).format(this._fiatTotal);
 return html`<div class="fiat approx">? ${formatted}</div>`;
 }
 return nothing;
 }

 private _renderDetails() {
 if (!this._invoice) return nothing;
 const ln = this._invoice.bolt11;
 const openHref = `lightning:${ln}`;
 return html`
 <div class="details">
 <button class="link" @click=${this._toggleDetails} aria-expanded=${this._detailsOpen ? 'true' : 'false'}>
 ${this._detailsOpen ? this.hideDetailsLabel : this.showDetailsLabel}
 </button>
 ${this._detailsOpen ? html`
 <div class="grid">
 <div><span class="k">Method</span><span class="v">${this._feeMethod ?? 'bolt11'}</span></div>
 ${this._feeAmount != null ? html`<div><span class="k">Amount</span><span class="v">${this._feeAmount.toLocaleString()} sats</span></div>` : nothing}
 ${this._feeSat != null ? html`<div><span class="k">Fees</span><span class="v">${this._feeSat.toLocaleString()} sats</span></div>` : nothing}
 ${this.expiry ? html`<div><span class="k">Expiry</span><span class="v">${new Date(this.expiry).toLocaleString()}</span></div>` : nothing}
 <div class="ln"><span class="k">Invoice</span><uui-textarea readonly .value=${ln}></uui-textarea></div>
 <div class="actions">
 <uui-button look="secondary" @click=${this._copyInvoice}>${this._copyOk ? this.copiedLabel : this.copyInvoiceLabel}</uui-button>
 <a class="open-wallet" href=${openHref}>${this.openWalletLabel}</a>
 </div>
 </div>
 ` : nothing}
 </div>`;
 }

 private _renderAltMethods() {
 const lnurlSection = this.enableLnurl ? html`
 <div class="alt">
 <uui-button look="secondary" @click=${this._toggleLnurl}>${this.lnurlLabel}</uui-button>
 ${this._lnurlOpen ? html`
 <div class="alt-body">
 ${this._lnurlLoading ? html`<div class="meta">${this.lnurlLoadingLabel}</div>` : this._lnurlError ? html`<div class="error" role="alert">${this._lnurlError}</div>` : this._lnurlValue ? html`
 <breez-qr-code-display .data=${this._lnurlValue}></breez-qr-code-display>
 <div class="mono">${this._lnurlValue}</div>
 ` : nothing}
 ${this.lightningAddress ? html`
 <div class="addr">
 <div class="addr-row"><span class="k">${this.lightningAddressLabel}</span><span class="v">${this.lightningAddress}</span></div>
 <uui-button look="secondary" @click=${this._copyAddress}>${this._addrCopyOk ? this.copiedLabel : this.copyAddressLabel}</uui-button>
 </div>` : nothing}
 </div>` : nothing}
 </div>` : nothing;

 const bolt12Section = this.enableBolt12 ? html`
 <div class="alt">
 ${!this._bolt12Open ? html`<uui-button look="secondary" @click=${this._toggleBolt12}>${this.bolt12Label}</uui-button>` : html`<uui-button look="secondary" @click=${this._toggleBolt12}>${this.backToInvoiceLabel}</uui-button>`}
 ${this._bolt12Open ? html`
 <div class="alt-body">
 ${this._bolt12Loading ? html`<div class="meta">${this.bolt12LoadingLabel}</div>` : this._bolt12Error ? html`<div class="error" role="alert">${this._bolt12Error}</div>` : this._bolt12Offer ? html`
 <breez-qr-code-display .data=${this._bolt12Offer}></breez-qr-code-display>
 <div class="mono">${this._bolt12Offer}</div>
 ` : nothing}
 </div>` : nothing}
 </div>` : nothing;

 return html`<div class="alts">${lnurlSection}${bolt12Section}</div>`;
 }

 private _renderFeeQuote() {
 if (this._feeLoading) return html`<div class="fees" role="status" aria-live="polite">${this.estimatingFeesLabel}</div>`;
 if (this._feeAmount != null && this._feeSat != null) {
 const total = this._feeAmount + this._feeSat;
 return html`
 <div class="fees">
 <div class="row"><span>Amount</span><span><strong>${this._feeAmount.toLocaleString()} sats</strong></span></div>
 <div class="row"><span>${this.feesLabel}${this._feeMethod ? html` (${this._feeMethod})` : nothing}:</span><span>${this._feeSat.toLocaleString()} sats</span></div>
 <div class="row total"><span>${this.totalLabel}</span><span><strong>${total.toLocaleString()} sats</strong></span></div>
 ${this._renderFiat()}
 </div>`;
 }
 return html`${this._renderFiat()}`;
 }

 render() {
 if (!this.open) return html``;
 const labelledBy = this._titleId;
 const describedBy = this.description ? this._descId : undefined;
 return html`
 <div class="overlay" @click=${this._handleClose}>
 <div class="modal" @click=${(e: Event) => e.stopPropagation()} role="dialog" aria-modal="true" aria-labelledby=${labelledBy} ${describedBy ? html`aria-describedby=${describedBy}` : nothing} tabindex="-1">
 <header class="header">
 <h3 id=${this._titleId}>${this.title}</h3>
 <button class="close" @click=${this._handleClose} aria-label=${this.closeLabel}>×</button>
 </header>
 <section class="body">
 ${this._error ? html`<div class="error" role="alert">${this._error}</div>` : nothing}
 ${!this._invoice
 ? html`
 ${this.description ? html`<p id=${this._descId} class="desc">${this.description}</p>` : nothing}
 ${this._renderFeeQuote()}
 <button class="primary" @click=${this._generateInvoice} ?disabled=${this._loading} aria-busy=${this._loading ? 'true' : 'false'}>
 ${this._loading ? this.generatingLabel : this.generateLabel}
 </button>
 `
 : html`
 <breez-qr-code-display .data=${this._invoice.bolt11}></breez-qr-code-display>
 <breez-invoice-display .invoice=${this._invoice.bolt11}></breez-invoice-display>
 ${this._weblnAvailable ? html`
 <div class="webln">
 <uui-button look="positive" @click=${this._payWithWebln} ?disabled=${this._weblnBusy}>${this.weblnLabel}</uui-button>
 ${this._weblnError ? html`<div class="error" role="alert">${this.weblnErrorLabel}: ${this._weblnError}</div>` : nothing}
 </div>` : nothing}
 ${this._remaining ? html`<div class="expiry" role="status" aria-live="polite">${this._remaining}</div>` : nothing}
 ${this._renderAltMethods()}
 ${this._renderDetails()}
 ${this._isExpired ? html`
 <button class="secondary" @click=${this._regenerateInvoice} ?disabled=${this._loading}>
 ${this.regenerateLabel}
 </button>
 ` : nothing}
 `}
 </section>
 </div>
 </div>`;
 }

 static styles = css`
 :host { display: block; }
 .overlay { position: fixed; inset:0; background: var(--lp-overlay); display:flex; align-items:center; justify-content:center; z-index:1000; }
 .modal { background: var(--lp-color-surface); color: var(--lp-color-text); border-radius: var(--lp-radius); width:min(520px,92vw); max-height:90vh; overflow:auto; box-shadow: var(--lp-shadow); outline:none; }
 .header { display:flex; justify-content:space-between; align-items:center; padding:0.75rem1rem; border-bottom: var(--lp-border); }
 .header h3 { margin:0; font-size:1.1rem; }
 .close { border:0; background:none; font-size:1.4rem; cursor:pointer; color: var(--lp-color-text); }
 .body { padding:1rem; display:flex; flex-direction:column; gap:0.75rem; }
 .primary { background: var(--lp-color-primary); border:0; color: var(--lp-color-bg); padding:0.7rem1rem; border-radius: var(--lp-radius); cursor:pointer; }
 .primary:hover { background: var(--lp-color-primary-hover); }
 .primary[aria-busy="true"] { opacity:0.8; cursor:wait; }
 .secondary { background: transparent; color: var(--lp-color-text); border: var(--lp-border); padding:0.5rem1rem; border-radius: var(--lp-radius); cursor:pointer; }
 .error { color: var(--lp-color-danger); padding:0.5rem0; }
 .desc { color: var(--lp-color-text-muted); }
 .expiry { color: var(--lp-color-text-muted); font-size:0.9rem; }
 .fees { border: var(--lp-border); border-radius: var(--lp-radius); padding:0.75rem; background: var(--lp-color-surface); color: var(--lp-color-text); }
 .fees .row { display:flex; justify-content:space-between; gap:0.5rem; padding:0.25rem0; }
 .fees .row.total { border-top: var(--lp-border); margin-top:0.25rem; padding-top:0.5rem; font-weight:600; }
 .fiat { margin-top:0.35rem; font-size:0.95rem; color: var(--lp-color-text-muted); }
 .fiat.error { color: var(--lp-color-danger); }
 .fiat .link { background:none; border:0; color: var(--lp-color-primary); cursor:pointer; text-decoration: underline; padding:0; }
 .details { margin-top:0.5rem; }
 .details .grid { display:grid; grid-template-columns:1fr1fr; gap:0.5rem1rem; align-items:start; }
 .details .k { color: var(--lp-color-text-muted); margin-right:0.35rem; }
 .details .ln { grid-column:1 / -1; }
 .details .actions { display:flex; gap:0.5rem; align-items:center; margin-top:0.25rem; }
 .open-wallet { text-decoration: none; background: var(--lp-color-primary); color: var(--lp-color-bg); padding:0.4rem0.6rem; border-radius:4px; }
 .webln { display:flex; align-items:center; gap:0.5rem; }
 .alts { display:flex; flex-direction:column; gap:0.5rem; }
 .alt { display:flex; flex-direction:column; gap:0.35rem; }
 .alt-body { display:flex; flex-direction:column; gap:0.35rem; }
 .mono { font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace; white-space: nowrap; overflow:auto; }
 .addr { display:flex; flex-direction:column; gap:0.35rem; }
 .addr-row { display:flex; gap:0.5rem; align-items:center; color: var(--lp-color-text-muted); }
 `;
}

declare global {
 interface HTMLElementTagNameMap {
 'breez-payment-modal': BreezPaymentModalBasic;
 }
}
