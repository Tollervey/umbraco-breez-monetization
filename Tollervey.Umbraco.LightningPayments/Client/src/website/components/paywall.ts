import { LitElement, html, css, nothing } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import './payment-modal.basic.js';

@customElement('breez-paywall')
export class BreezPaywallElement extends LitElement {
 @property({ type: Number, attribute: 'content-id' }) contentId!: number;
 @property({ type: String }) buttonLabel: string = 'Pay to unlock';
 @property({ type: String }) title: string = 'Unlock content';
 @property({ type: String }) description: string = 'One-time Lightning payment to access this content.';

 @state() private _status: 'unknown' | 'paid' | 'pending' | 'failed' | 'expired' | 'unpaid' = 'unknown';
 @state() private _loading: boolean = true;
 @state() private _error: string = '';
 @state() private _modalOpen: boolean = false;

 private _evtSrc?: EventSource;

 connectedCallback(): void { super.connectedCallback(); this._checkStatus(); this._connectRealtime(); }
 disconnectedCallback(): void { super.disconnectedCallback(); this._evtSrc?.close(); this._evtSrc = undefined; }

 private _connectRealtime() {
 try {
 this._evtSrc?.close();
 this._evtSrc = new EventSource('/api/public/lightning/realtime/subscribe');
 this._evtSrc.addEventListener('payment-succeeded', (ev: MessageEvent) => {
 try {
 const data = JSON.parse(ev.data);
 if (typeof data?.contentId === 'number' && data.contentId === this.contentId) {
 this._status = 'paid';
 this._modalOpen = false;
 }
 } catch { /* ignore */ }
 });
 this._evtSrc.onerror = () => { /* keep retrying */ };
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
 finally { this._loading = false; if (this._status === 'paid') { this.dispatchEvent(new CustomEvent('breez-unlocked', { bubbles: true, composed: true })); } }
 }

 private _openModal() { this._modalOpen = true; this._status = 'pending'; }
 private _closeModal = () => { this._modalOpen = false; };
 private _onInvoiceGenerated = (_e: CustomEvent) => { this._status = 'pending'; };

 render() {
 if (this._status === 'paid') { return html`<slot></slot>`; }
 if (this._loading || this._status === 'unknown') { return html`<div class="breez-paywall loading">Checking access…</div>`; }
 const problem = this._status === 'failed' ? 'Payment failed. Please try again.' : this._status === 'expired' ? 'Invoice expired. Generate a new one.' : '';
 return html`
 <div class="breez-paywall ${this._status}">
 ${this._error ? html`<div class="error">${this._error}</div>` : nothing}
 ${problem ? html`<div class="warning">${problem}</div>` : nothing}
 ${this._status !== 'pending'
 ? html`<button class="primary" @click=${this._openModal}>${this.buttonLabel}</button>`
 : html`<div class="pending">Waiting for payment confirmation…</div>`}
 </div>
 <breez-payment-modal .open=${this._modalOpen} .contentId=${this.contentId} .amount=${0} .title=${this.title} .description=${this.description} @close=${this._closeModal} @invoice-generated=${this._onInvoiceGenerated}></breez-payment-modal>
 `;
 }

 static styles = css`
 :host { display: block; }
 .breez-paywall { display:flex; align-items:center; gap:0.5rem; flex-wrap: wrap; }
 .loading { color:#666; }
 .pending { color:#666; }
 .warning { color:#a15c00; }
 .error { color:#b00020; }
 .primary { background:#f89c1c; border:0; color:white; padding:0.5rem0.9rem; border-radius:6px; cursor:pointer; }
 .primary:hover { background:#e68a0a; }
 `;
}

declare global { interface HTMLElementTagNameMap { 'breez-paywall': BreezPaywallElement; } }
