import { LitElement, html, css } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import './qr-code-display.js';
import './invoice-display.js';

@customElement('breez-payment-modal')
export class BreezPaymentModalBasic extends LitElement {
 @property({ type: Boolean }) open = false;
 @property({ type: Number }) amount =0;
 @property({ type: String }) title = 'Lightning Payment';
 @property({ type: String }) description = '';
 @property({ type: Number }) contentId?: number;
 // allow passing a pre-created invoice and hash (used by tip-jar)
 @property({ type: String }) invoice?: string;
 @property({ type: String }) paymentHash?: string;

 @state() private _invoice?: { bolt11: string; paymentHash: string };
 @state() private _loading = false;
 @state() private _error = '';

 private _emitClose() {
 this.dispatchEvent(new CustomEvent('close', { bubbles: true, composed: true }));
 }

 private _handleClose = () => {
 this.open = false;
 this._emitClose();
 };

 async _generateInvoice() {
 if (!this.contentId || this.amount <=0) {
 this._error = 'Missing content id or amount';
 return;
 }
 this._loading = true;
 this._error = '';
 try {
 const res = await fetch(`/api/public/lightning/GetPaywallInvoice?contentId=${this.contentId}`);
 if (!res.ok) throw new Error(`HTTP ${res.status}`);
 const data = await res.json();
 this._invoice = { bolt11: data.invoice, paymentHash: data.paymentHash };
 // notify host that we have an active invoice
 this.dispatchEvent(new CustomEvent('invoice-generated', { detail: { paymentHash: data.paymentHash }, bubbles: true, composed: true }));
 } catch (err: any) {
 this._error = err?.message ?? 'Failed to create invoice';
 } finally {
 this._loading = false;
 }
 }

 updated(changed: Map<string, unknown>) {
 if (changed.has('open') || changed.has('invoice') || changed.has('paymentHash')) {
 if (this.open && this.invoice && this.paymentHash && !this._invoice) {
 this._invoice = { bolt11: this.invoice, paymentHash: this.paymentHash };
 this.dispatchEvent(new CustomEvent('invoice-generated', { detail: { paymentHash: this.paymentHash }, bubbles: true, composed: true }));
 }
 }
 }

 render() {
 if (!this.open) return html``;
 return html`
 <div class="overlay" @click=${this._handleClose}>
 <div class="modal" @click=${(e: Event) => e.stopPropagation()} role="dialog" aria-modal="true">
 <header class="header">
 <h3>${this.title}</h3>
 <button class="close" @click=${this._handleClose} aria-label="Close">×</button>
 </header>
 <section class="body">
 ${this._error ? html`<div class="error">${this._error}</div>` : ''}
 ${!this._invoice
 ? html`
 <p class="desc">${this.description}</p>
 <button class="primary" @click=${this._generateInvoice} ?disabled=${this._loading}>
 ${this._loading ? 'Generating...' : 'Generate Invoice'}
 </button>
 `
 : html`
 <breez-qr-code-display .data=${this._invoice.bolt11}></breez-qr-code-display>
 <breez-invoice-display .invoice=${this._invoice.bolt11}></breez-invoice-display>
 `}
 </section>
 </div>
 </div>
 `;
 }

 static styles = css`
 :host { display: block; }
 .overlay { position: fixed; inset:0; background: rgba(0,0,0,0.6); display:flex; align-items:center; justify-content:center; z-index:1000; }
 .modal { background: white; color: #222; border-radius:8px; width: min(520px,92vw); max-height:90vh; overflow:auto; box-shadow:010px30px rgba(0,0,0,0.3); }
 .header { display:flex; justify-content: space-between; align-items:center; padding:0.75rem1rem; border-bottom:1px solid #eee; }
 .header h3 { margin:0; font-size:1.1rem; }
 .close { border:0; background: none; font-size:1.4rem; cursor: pointer; }
 .body { padding:1rem; display:flex; flex-direction: column; gap:0.75rem; }
 .primary { background: #f89c1c; border:0; color: white; padding:0.7rem1rem; border-radius:6px; cursor: pointer; }
 .primary:hover { background: #e68a0a; }
 .error { color: #b00020; padding:0.5rem0; }
 .desc { color: #666; }
 `;
}

declare global {
 interface HTMLElementTagNameMap {
 'breez-payment-modal': BreezPaymentModalBasic;
 }
}
