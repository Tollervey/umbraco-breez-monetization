import { LitElement, html, css } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import './payment-modal.basic.js';

/**
 * <breez-paywall>
 * - Checks payment status for the given contentId.
 * - If unpaid, shows a button to open the payment modal.
 * - Polls payment status while modal is open and reloads on success.
 */
@customElement('breez-paywall')
export class BreezPaywallElement extends LitElement {
 @property({ type: Number, attribute: 'content-id' }) contentId!: number;
 @property({ type: String }) buttonLabel: string = 'Pay to unlock';
 @property({ type: String }) title: string = 'Unlock content';
 @property({ type: String }) description: string = 'One-time Lightning payment to access this content.';

 @state() private _paid: boolean = false;
 @state() private _loading: boolean = true;
 @state() private _error: string = '';
 @state() private _modalOpen: boolean = false;

 private _pollTimer: number | null = null;

 connectedCallback(): void {
 super.connectedCallback();
 this._checkStatus();
 }

 disconnectedCallback(): void {
 super.disconnectedCallback();
 this._stopPolling();
 }

 private async _checkStatus() {
 if (!this.contentId || this.contentId <=0) {
 this._error = 'Invalid content id';
 this._loading = false;
 return;
 }
 this._loading = true;
 this._error = '';
 try {
 const res = await fetch(`/api/public/lightning/GetPaymentStatus?contentId=${this.contentId}`);
 if (!res.ok) {
 //401 means no cookie yet, treat as unpaid
 if (res.status !==401) throw new Error(`HTTP ${res.status}`);
 this._paid = false;
 } else {
 const data = await res.json();
 this._paid = (data?.status || '').toLowerCase() === 'paid';
 }
 } catch (err: any) {
 console.error('Failed to check payment status', err);
 this._error = err?.message ?? 'Failed to check payment status';
 } finally {
 this._loading = false;
 if (this._paid) {
 // Optionally notify and reload so server-side middleware allows the full content
 this.dispatchEvent(new CustomEvent('breez-unlocked', { bubbles: true, composed: true }));
 // Give a small delay for UX before reload
 setTimeout(() => window.location.reload(),500);
 }
 }
 }

 private _startPolling() {
 this._stopPolling();
 this._pollTimer = window.setInterval(() => this._checkStatus(),2000);
 }

 private _stopPolling() {
 if (this._pollTimer) {
 clearInterval(this._pollTimer);
 this._pollTimer = null;
 }
 }

 private _openModal() {
 this._modalOpen = true;
 // Begin polling for status while modal is open
 this._startPolling();
 }

 private _closeModal() {
 this._modalOpen = false;
 // Keep polling briefly in case payment just settled; stop if user closes
 this._stopPolling();
 }

 render() {
 if (this._loading) {
 return html`<div class="breez-paywall loading">Checking access…</div>`;
 }

 if (this._paid) {
 return html`<div class="breez-paywall unlocked">Access granted ?</div>`;
 }

 return html`
 <div class="breez-paywall locked">
 ${this._error ? html`<div class="error">${this._error}</div>` : ''}
 <button class="primary" @click=${this._openModal}>${this.buttonLabel}</button>
 </div>

 <breez-payment-modal
 .open=${this._modalOpen}
 .contentId=${this.contentId}
 .amount=${0}
 .title=${this.title}
 .description=${this.description}
 @close=${this._closeModal}
 ></breez-payment-modal>
 `;
 }

 static styles = css`
 :host { display: block; }
 .breez-paywall { display: flex; align-items: center; gap:0.5rem; }
 .loading { color: #666; }
 .unlocked { color: #2e7d32; }
 .locked { color: #333; }
 .error { color: #b00020; }
 .primary { background: #f89c1c; border:0; color: white; padding:0.5rem0.9rem; border-radius:6px; cursor: pointer; }
 .primary:hover { background: #e68a0a; }
 `;
}

declare global {
 interface HTMLElementTagNameMap {
 'breez-paywall': BreezPaywallElement;
 }
}
