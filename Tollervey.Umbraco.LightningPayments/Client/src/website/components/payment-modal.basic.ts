import { LitElement, html, css, nothing } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import './qr-code-display.js';
import './invoice-display.js';

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

 // Localizable/Configurable labels
 @property({ type: String, attribute: 'generate-label' }) generateLabel = 'Generate Invoice';
 @property({ type: String, attribute: 'generating-label' }) generatingLabel = 'Generating…';
 @property({ type: String, attribute: 'close-label' }) closeLabel = 'Close';
 @property({ type: String, attribute: 'missing-content-label' }) missingContentLabel = 'Missing or invalid content id';
 @property({ type: String, attribute: 'create-error-label' }) createErrorLabel = 'Failed to create invoice';
 @property({ type: String, attribute: 'expires-in-label' }) expiresInLabel = 'Expires in';
 @property({ type: String, attribute: 'expired-label' }) expiredLabel = 'Invoice expired. Generate a new one.';

 @state() private _invoice?: { bolt11: string; paymentHash: string };
 @state() private _loading = false;
 @state() private _error = '';
 @state() private _remaining = '';
 private _countdownTimer: number | null = null;

 private _previouslyFocused: Element | null = null;
 private _titleId = `bpm-title-${Math.random().toString(36).slice(2)}`;
 private _descId = `bpm-desc-${Math.random().toString(36).slice(2)}`;

 private _emitClose() {
 this.dispatchEvent(new CustomEvent('close', { bubbles: true, composed: true }));
 }

 private _handleClose = () => {
 this.open = false;
 this._emitClose();
 };

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
 const active = this.shadowRoot?.activeElement as HTMLElement | null ?? (document.activeElement as HTMLElement | null);
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
 const container = this.shadowRoot?.querySelector('.modal') as HTMLElement | null;
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
 const modal = this.shadowRoot?.querySelector('.modal') as HTMLElement | null;
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
 return;
 }
 const expiryTs = Date.parse(this.expiry);
 if (isNaN(expiryTs)) {
 this._remaining = '';
 return;
 }
 const now = Date.now();
 const ms = expiryTs - now;
 if (ms <=0) {
 this._remaining = this.expiredLabel;
 this._clearCountdown();
 return;
 }
 const totalSec = Math.floor(ms /1000);
 const m = Math.floor(totalSec /60);
 const s = totalSec %60;
 this._remaining = `${this.expiresInLabel} ${m}:${s.toString().padStart(2,'0')}`;
 }

 private _startCountdown() {
 this._clearCountdown();
 this._updateCountdown();
 this._countdownTimer = window.setInterval(() => this._updateCountdown(),1000);
 }

 private _clearCountdown() {
 if (this._countdownTimer) {
 clearInterval(this._countdownTimer);
 this._countdownTimer = null;
 }
 }

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
 this.dispatchEvent(new CustomEvent('invoice-generated', { detail: { paymentHash: data.paymentHash }, bubbles: true, composed: true }));
 } catch (err: any) {
 this._error = err?.message ?? this.createErrorLabel;
 } finally {
 this._loading = false;
 }
 }

 updated(changed: Map<string, unknown>) {
 if (changed.has('open')) {
 if (this.open) {
 this._previouslyFocused = (this.getRootNode() as Document | ShadowRoot).activeElement as Element | null;
 this._lockScroll(true);
 // Defer to ensure DOM is rendered
 setTimeout(() => this._focusInitial(),0);
 this.addEventListener('keydown', this._onKeyDown);
 if (this.expiry) this._startCountdown();
 } else {
 this._lockScroll(false);
 this.removeEventListener('keydown', this._onKeyDown);
 this._clearCountdown();
 if (this._previouslyFocused && (this._previouslyFocused as HTMLElement).focus) {
 (this._previouslyFocused as HTMLElement).focus();
 }
 }
 }

 if (changed.has('open') || changed.has('invoice') || changed.has('paymentHash')) {
 if (this.open && this.invoice && this.paymentHash && !this._invoice) {
 this._invoice = { bolt11: this.invoice, paymentHash: this.paymentHash };
 this.dispatchEvent(new CustomEvent('invoice-generated', { detail: { paymentHash: this.paymentHash }, bubbles: true, composed: true }));
 }
 }
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
 <button class="primary" @click=${this._generateInvoice} ?disabled=${this._loading} aria-busy=${this._loading ? 'true' : 'false'}>
 ${this._loading ? this.generatingLabel : this.generateLabel}
 </button>
 `
 : html`
 <breez-qr-code-display .data=${this._invoice.bolt11}></breez-qr-code-display>
 <breez-invoice-display .invoice=${this._invoice.bolt11}></breez-invoice-display>
 ${this._remaining ? html`<div class="expiry" role="status" aria-live="polite">${this._remaining}</div>` : nothing}
 `}
 </section>
 </div>
 </div>`;
 }

 static styles = css`
 :host { display: block; }
 .overlay { position: fixed; inset:0; background: rgba(0,0,0,0.6); display:flex; align-items:center; justify-content:center; z-index:1000; }
 .modal { background:white; color:#222; border-radius:8px; width:min(520px,92vw); max-height:90vh; overflow:auto; box-shadow:010px30px rgba(0,0,0,0.3); outline:none; }
 .header { display:flex; justify-content:space-between; align-items:center; padding:0.75rem1rem; border-bottom:1px solid #eee; }
 .header h3 { margin:0; font-size:1.1rem; }
 .close { border:0; background:none; font-size:1.4rem; cursor:pointer; }
 .body { padding:1rem; display:flex; flex-direction:column; gap:0.75rem; }
 .primary { background:#f89c1c; border:0; color:white; padding:0.7rem1rem; border-radius:6px; cursor:pointer; }
 .primary:hover { background:#e68a0a; }
 .primary[aria-busy="true"] { opacity:0.8; cursor:wait; }
 .error { color:#b00020; padding:0.5rem0; }
 .desc { color:#666; }
 .expiry { color:#666; font-size:0.9rem; }
 `;
}

declare global {
 interface HTMLElementTagNameMap {
 'breez-payment-modal': BreezPaymentModalBasic;
 }
}
