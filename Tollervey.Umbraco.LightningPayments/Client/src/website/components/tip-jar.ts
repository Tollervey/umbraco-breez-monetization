import { LitElement, html, css } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import './payment-modal.basic.js';

/**
 * <breez-tip-jar>
 * - Lets a user select an amount and creates a tip invoice via the public API.
 * - Reuses the payment modal to present the QR/invoice.
 * - Shows basic stats fetched from a read-only endpoint.
 */
@customElement('breez-tip-jar')
export class BreezTipJarElement extends LitElement {
 @property({ type: Number }) defaultAmount: number =1000;
 @property({ type: Array }) amounts: number[] = [500,1000,2500,5000];
 @property({ type: Number }) contentId?: number; // optional content association
 @property({ type: String }) title: string = 'Send a tip';
 @property({ type: String }) description: string = 'Thank you for supporting!';

 // i18n/configurable labels
 @property({ type: String, attribute: 'tip-button-label' }) tipButtonLabel = 'Tip';
 @property({ type: String, attribute: 'please-wait-label' }) pleaseWaitLabel = 'Please wait…';
 @property({ type: String, attribute: 'thanks-label' }) thanksLabel = 'Thanks for your tip!';
 @property({ type: String, attribute: 'stats-error-label' }) statsErrorLabel = 'Failed to load stats';
 @property({ type: String, attribute: 'refresh-label' }) refreshLabel = 'Refresh status';
 @property({ type: String, attribute: 'custom-amount-label' }) customAmountLabel = 'Custom amount';
 @property({ type: String, attribute: 'tip-stats-label' }) tipStatsLabel = '{count} tips, {total} sats total';
 @property({ type: String, attribute: 'pending-hint-label' }) pendingHintLabel = 'Waiting for payment confirmation…';
 @property({ type: String, attribute: 'offline-hint-label' }) offlineHintLabel = 'Live updates offline, retrying…';

 @state() private _selected: number =1000;
 @state() private _loading = false;
 @state() private _error = '';
 @state() private _modalOpen = false;
 @state() private _bolt11?: string;
 @state() private _paymentHash?: string;

 // basic stats
 @state() private _totalSats: number | null = null;
 @state() private _count: number | null = null;
 @state() private _statsError = '';

 // realtime
 private _evtSrc?: EventSource;
 @state() private _thanks = false;
 private _pollTimer: number | null = null;
 private readonly _pollMs =2000;
 @state() private _rtConnected = true;
 @state() private _rtAttempts =0;

 connectedCallback(): void {
 super.connectedCallback();
 this._selected = this.defaultAmount || this.amounts[0] ||1000;
 this.loadStats();
 this._connectRealtime();
 }

 disconnectedCallback(): void {
 super.disconnectedCallback();
 if (this._evtSrc) { this._evtSrc.close(); this._evtSrc = undefined; }
 if (this._pollTimer) { clearInterval(this._pollTimer); this._pollTimer = null; }
 }

 private _connectRealtime() {
 try {
 this._evtSrc?.close();
 this._evtSrc = new EventSource('/api/public/lightning/realtime/subscribe');
 this._evtSrc.onopen = () => { this._rtConnected = true; this._rtAttempts =0; };
 this._evtSrc.addEventListener('payment-succeeded', (ev: MessageEvent) => {
 try {
 const data = JSON.parse(ev.data);
 // If this event references the same hash we created here, show thanks.
 if (data?.paymentHash && data.paymentHash === this._paymentHash) {
 this._thanks = true;
 this._modalOpen = false;
 this._stopPolling();
 // Optionally refresh stats
 this.loadStats();
 }
 } catch { /* ignore */ }
 });
 this._evtSrc.onerror = () => {
 this._rtConnected = false; this._rtAttempts++;
 const backoff = Math.min(30000,1000 * Math.pow(2, this._rtAttempts));
 console.warn(`[tip-jar] realtime disconnected, attempt ${this._rtAttempts}, next retry ~${Math.round(backoff/1000)}s`);
 };
 } catch { /* ignore */ }
 }

 private _startPolling() {
 this._stopPolling();
 if (!this._paymentHash) return;
 this._pollTimer = window.setInterval(async () => {
 try {
 const res = await fetch(`/api/public/lightning/GetPaymentStatusByHash?paymentHash=${encodeURIComponent(this._paymentHash!)}`);
 if (!res.ok) return;
 const data = await res.json();
 const s = (data?.status || '').toLowerCase();
 if (s === 'paid') {
 this._thanks = true;
 this._modalOpen = false;
 this._stopPolling();
 // Optionally refresh stats
 this.loadStats();
 }
 } catch { /* ignore transient */ }
 }, this._pollMs);
 }

 private _stopPolling() { if (this._pollTimer) { clearInterval(this._pollTimer); this._pollTimer = null; } }

 private async loadStats() {
 const url = this.contentId ? `/api/public/lightning/GetTipStats?contentId=${this.contentId}` : `/api/public/lightning/GetTipStats`;
 try {
 const res = await fetch(url);
 if (!res.ok) return;
 const data = await res.json();
 this._totalSats = typeof data.totalSats === 'number' ? data.totalSats : null;
 this._count = typeof data.count === 'number' ? data.count : null;
 } catch (e: any) {
 this._statsError = e?.message ?? this.statsErrorLabel;
 }
 }

 private async _createTip() {
 this._loading = true;
 this._error = '';
 this._bolt11 = undefined;
 this._paymentHash = undefined;
 this._thanks = false;
 try {
 const res = await fetch('/api/public/lightning/CreateTipInvoice', {
 method: 'POST',
 headers: { 'Content-Type': 'application/json' },
 body: JSON.stringify({ amountSat: this._selected, contentId: this.contentId ?? null, label: 'Tip jar' })
 });
 if (!res.ok) throw new Error(`HTTP ${res.status}`);
 const data = await res.json();
 this._bolt11 = data.invoice;
 this._paymentHash = data.paymentHash;
 this._modalOpen = true;
 this._startPolling();
 } catch (err: any) {
 this._error = err?.message ?? 'Failed to create tip invoice';
 } finally {
 this._loading = false;
 }
 }

 private _refreshNow = async () => {
 if (!this._paymentHash) return;
 try {
 const res = await fetch(`/api/public/lightning/GetPaymentStatusByHash?paymentHash=${encodeURIComponent(this._paymentHash)}`);
 if (!res.ok) return;
 const data = await res.json();
 const s = (data?.status || '').toLowerCase();
 if (s === 'paid') {
 this._thanks = true;
 this._modalOpen = false;
 this._stopPolling();
 this.loadStats();
 }
 } catch { /* ignore */ }
 };

 render() {
 return html`
 <div class="tip-jar">
 <div class="row" role="group" aria-label="${this.customAmountLabel}">
 ${this.amounts.map(a => html`
 <button class="amount-btn" @click=${() => this._selected = a} ?data-selected=${this._selected === a} aria-pressed=${this._selected === a ? 'true' : 'false'}>${a.toLocaleString()} sats</button>
 `)}
 <label class="custom-input">
 <span class="sr-only">${this.customAmountLabel}</span>
 <input type="number" min="1" step="1" .value=${this._selected} @input=${(e: any) => this._selected = Math.max(1, parseInt(e.target.value ||0))} />
 <span aria-hidden="true">sats</span>
 </label>
 </div>
 ${this._statsError ? html`<div class="stats error" role="alert" aria-live="assertive">${this._statsError}</div>` : ''}
 ${this._totalSats != null && this._count != null ? html`<div class="stats" role="status" aria-live="polite">${this.tipStatsLabel.replace('{count}', String(this._count)).replace('{total}', this._totalSats.toLocaleString())}</div>` : ''}
 ${this._thanks ? html`<div class="thanks" role="status" aria-live="polite">${this.thanksLabel} ??</div>` : ''}
 ${this._error ? html`<div class="error" role="alert" aria-live="assertive">${this._error}</div>` : ''}
 <button class="primary" @click=${this._createTip} ?disabled=${this._loading} aria-busy=${this._loading ? 'true' : 'false'}>${this._loading ? this.pleaseWaitLabel : this.tipButtonLabel}</button>
 </div>

 <breez-payment-modal
 .open=${this._modalOpen}
 .title=${this.title}
 .description=${this.description}
 .invoice=${this._bolt11}
 .paymentHash=${this._paymentHash}
 .enableBolt12=${true}
 .enableLnurl=${true}
 @close=${() => { this._modalOpen = false; this._stopPolling(); }}
 ></breez-payment-modal>

 ${this._bolt11 && !this._thanks ? html`
 <div class="pending-tools" role="region" aria-live="polite">
 <div class="meta" role="status" aria-live="polite">${this.pendingHintLabel}</div>
 <button class="refresh" @click=${this._refreshNow}>${this.refreshLabel}</button>
 ${!this._rtConnected ? html`<div class="meta" role="status" aria-live="polite">${this.offlineHintLabel} (attempt ${this._rtAttempts})</div>` : ''}
 </div>
 ` : ''}
 `;
 }

 static styles = css`
 :host { display: block; }
 .sr-only { position:absolute; width:1px; height:1px; padding:0; margin:-1px; overflow:hidden; clip:rect(0,0,0,0); border:0; }
 .tip-jar { display: flex; flex-direction: column; gap:0.75rem; }
 .row { display: flex; flex-wrap: wrap; gap:0.5rem; align-items: center; }
 .amount-btn { background: #f1f1f1; border:1px solid #ddd; border-radius:6px; padding:0.4rem0.7rem; cursor: pointer; }
 .amount-btn[data-selected="true"], .amount-btn[data-selected] { background: #f89c1c; color: white; border-color: #e68a0a; }
 .custom-input { display: flex; align-items: center; gap:0.3rem; }
 .custom-input input { width:120px; padding:0.4rem; border:1px solid #ddd; border-radius:6px; }
 .primary { background: #f89c1c; border:0; color: white; padding:0.6rem1rem; border-radius:6px; cursor: pointer; align-self: start; }
 .primary:hover { background: #e68a0a; }
 .error { color: #b00020; }
 .thanks { color: var(--lp-color-success, #2e7d32); font-weight:600; }
 .stats { color: #666; font-size:0.9rem; }
 .pending-tools { margin-top:0.5rem; display:flex; gap:0.5rem; align-items:center; }
 .refresh { background: transparent; border:1px solid #ddd; border-radius:6px; padding:0.3rem0.6rem; cursor: pointer; }
 .meta { color: #666; font-size:0.85rem; }
 `;
}

declare global {
 interface HTMLElementTagNameMap {
 'breez-tip-jar': BreezTipJarElement;
 }
}
