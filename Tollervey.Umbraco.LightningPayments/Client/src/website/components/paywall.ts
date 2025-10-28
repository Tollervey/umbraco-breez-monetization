import { LitElement, html, css, nothing } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import './payment-modal.basic.js';

@customElement('breez-paywall')
export class BreezPaywallElement extends LitElement {
 @property({ type: Number, attribute: 'content-id' }) contentId!: number;
 @property({ type: String }) buttonLabel: string = 'Pay to unlock';
 @property({ type: String }) title: string = 'Unlock content';
 @property({ type: String }) description: string = 'One-time Lightning payment to access this content.';

 // labels
 @property({ type: String, attribute: 'checking-label' }) checkingLabel = 'Checking access…';
 @property({ type: String, attribute: 'waiting-label' }) waitingLabel = 'Waiting for payment confirmation…';
 @property({ type: String, attribute: 'failed-label' }) failedLabel = 'Payment failed. Please try again.';
 @property({ type: String, attribute: 'expired-label' }) expiredLabel = 'Invoice expired. Generate a new one.';
 @property({ type: String, attribute: 'refresh-label' }) refreshLabel = 'Refresh status';

 @state() private _status: 'unknown' | 'paid' | 'pending' | 'failed' | 'expired' | 'unpaid' = 'unknown';
 @state() private _loading: boolean = true;
 @state() private _error: string = '';
 @state() private _modalOpen: boolean = false;

 private _evtSrc?: EventSource;
 private _pollTimer: number | null = null;
 private readonly _pollMs =2000;
 @state() private _rtConnected = true;
 @state() private _rtAttempts =0;

 connectedCallback(): void { super.connectedCallback(); this._checkStatus(); this._connectRealtime(); }
 disconnectedCallback(): void { super.disconnectedCallback(); this._evtSrc?.close(); this._evtSrc = undefined; this._stopPolling(); }

 private _connectRealtime() {
 try {
 this._evtSrc?.close();
 this._evtSrc = new EventSource('/api/public/lightning/realtime/subscribe');
 this._evtSrc.onopen = () => { this._rtConnected = true; this._rtAttempts =0; };
 this._evtSrc.addEventListener('payment-succeeded', (ev: MessageEvent) => {
 try {
 const data = JSON.parse(ev.data);
 if (typeof data?.contentId === 'number' && data.contentId === this.contentId) {
 this._status = 'paid';
 this._modalOpen = false;
 this._stopPolling();
 }
 } catch { /* ignore */ }
 });
 this._evtSrc.onerror = () => {
 this._rtConnected = false; this._rtAttempts++;
 const backoff = Math.min(30000,1000 * Math.pow(2, this._rtAttempts));
 console.warn(`[paywall] realtime disconnected, attempt ${this._rtAttempts}, next retry ~${Math.round(backoff/1000)}s`);
 };
 } catch { /* ignore */ }
 }

 private async _checkStatus() {
 if (!this.contentId || this.contentId <=0) { this._error = 'Invalid content id'; this._loading = false; return; }
 this._loading = true; this._error = '';
 try {
 const res = await fetch(`/api/public/lightning/GetPaymentStatus?contentId=${this.contentId}`);
 if (!res.ok) { if (res.status ===401) { this._status = 'unpaid'; } else { throw new Error(`HTTP ${res.status}`); } }
 else { const data = await res.json(); const s = (data?.status || '').toLowerCase(); this._status = s === 'paid' ? 'paid' : s === 'failed' ? 'failed' : s === 'expired' ? 'expired' : 'unpaid'; }
 } catch (err: any) { console.error('Failed to check payment status', err); this._error = err?.message ?? 'Failed to check payment status'; }
 finally { this._loading = false; if (this._status === 'paid') { (this as unknown as HTMLElement).dispatchEvent(new CustomEvent('breez-unlocked', { bubbles: true, composed: true })); } }
 }

 private _startPolling() {
 this._stopPolling();
 if (!this.contentId || this.contentId <=0) return;
 this._pollTimer = window.setInterval(async () => {
 try {
 const res = await fetch(`/api/public/lightning/GetPaymentStatus?contentId=${this.contentId}`);
 if (!res.ok) return;
 const data = await res.json();
 const s = (data?.status || '').toLowerCase();
 if (s === 'paid') {
 this._status = 'paid';
 this._modalOpen = false;
 this._stopPolling();
 (this as unknown as HTMLElement).dispatchEvent(new CustomEvent('breez-unlocked', { bubbles: true, composed: true }));
 }
 } catch { /* ignore transient */ }
 }, this._pollMs);
 }

 private _stopPolling() {
 if (this._pollTimer) { clearInterval(this._pollTimer); this._pollTimer = null; }
 }

 private _refreshNow = async () => {
 await this._checkStatus();
 if (this._status === 'paid') { this._modalOpen = false; this._stopPolling(); }
 };

 private _openModal() { this._modalOpen = true; this._status = 'pending'; this._startPolling(); }
 private _closeModal = () => { this._modalOpen = false; this._stopPolling(); };
 private _onInvoiceGenerated = (_e: CustomEvent) => { this._status = 'pending'; this._startPolling(); };

 render() {
 if (this._status === 'paid') {
 return html`<slot></slot>`;
 }
 if (this._loading || this._status === 'unknown') {
 return html`<div class="breez-paywall loading" role="status" aria-live="polite">${this.checkingLabel}</div>`;
 }
 const problem = this._status === 'failed' ? this.failedLabel : this._status === 'expired' ? this.expiredLabel : '';
 return html`
 <div class="breez-paywall ${this._status}">
 ${this._error ? html`<div class="error" role="alert">${this._error}</div>` : nothing}
 ${problem ? html`<div class="warning" role="status" aria-live="polite">${problem}</div>` : nothing}
 ${this._status !== 'pending'
 ? html`<button class="primary" @click=${this._openModal}>${this.buttonLabel}</button>`
 : html`
 <div class="pending-wrap">
 <div class="pending" role="status" aria-live="polite">${this.waitingLabel}</div>
 <button class="refresh" @click=${this._refreshNow}>${this.refreshLabel}</button>
 ${!this._rtConnected ? html`<div class="meta" aria-live="polite">Live updates offline, retrying… (attempt ${this._rtAttempts})</div>` : nothing}
 </div>
 `}
 </div>
 <breez-payment-modal
 .open=${this._modalOpen}
 .contentId=${this.contentId}
 .amount=${0}
 .title=${this.title}
 .description=${this.description}
 .enableBolt12=${true}
 .enableLnurl=${true}
 @close=${this._closeModal}
 @invoice-generated=${this._onInvoiceGenerated}
 ></breez-payment-modal>
 `;
 }

 static styles = css`
 :host { display: block; }
 .breez-paywall { display:flex; align-items:center; gap:0.5rem; flex-wrap: wrap; }
 .pending-wrap { display:flex; align-items:center; gap:0.5rem; flex-wrap: wrap; }
 .loading { color: var(--lp-color-text-muted); }
 .pending { color: var(--lp-color-text-muted); }
 .warning { color: #a15c00; }
 .error { color: var(--lp-color-danger); }
 .primary { background: var(--lp-color-primary); border:0; color: var(--lp-color-bg); padding:0.5rem0.9rem; border-radius: var(--lp-radius); cursor:pointer; }
 .primary:hover { background: var(--lp-color-primary-hover); }
 .refresh { background: transparent; border: var(--lp-border); border-radius: var(--lp-radius); padding:0.3rem0.6rem; cursor: pointer; }
 .meta { color: var(--lp-color-text-muted); font-size:0.85rem; }
 `;
}

declare global { interface HTMLElementTagNameMap { 'breez-paywall': BreezPaywallElement; } }
