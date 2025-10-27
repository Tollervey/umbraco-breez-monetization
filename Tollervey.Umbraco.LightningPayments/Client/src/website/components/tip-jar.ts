import { LitElement, html, css } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import './payment-modal.basic.js';

/**
 * <breez-tip-jar>
 * - Lets a user select an amount and creates a tip invoice via the public API.
 * - Reuses the payment modal to present the QR/invoice.
 */
@customElement('breez-tip-jar')
export class BreezTipJarElement extends LitElement {
 @property({ type: Number }) defaultAmount: number =1000;
 @property({ type: Array }) amounts: number[] = [500,1000,2500,5000];
 @property({ type: Number }) contentId?: number; // optional content association
 @property({ type: String }) title: string = 'Send a tip';
 @property({ type: String }) description: string = 'Thank you for supporting!';

 @state() private _selected: number =1000;
 @state() private _loading = false;
 @state() private _error = '';
 @state() private _modalOpen = false;
 @state() private _bolt11?: string;
 @state() private _paymentHash?: string;

 connectedCallback(): void {
 super.connectedCallback();
 this._selected = this.defaultAmount || this.amounts[0] ||1000;
 }

 private async _createTip() {
 this._loading = true;
 this._error = '';
 this._bolt11 = undefined;
 this._paymentHash = undefined;
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
 } catch (err: any) {
 this._error = err?.message ?? 'Failed to create tip invoice';
 } finally {
 this._loading = false;
 }
 }

 render() {
 return html`
 <div class="tip-jar">
 <div class="row">
 ${this.amounts.map(a => html`
 <button class="amount-btn" @click=${() => this._selected = a} ?data-selected=${this._selected === a}>${a.toLocaleString()} sats</button>
 `)}
 <label class="custom-input">
 <input type="number" min="1" step="1" .value=${this._selected} @input=${(e: any) => this._selected = Math.max(1, parseInt(e.target.value ||0))} />
 <span>sats</span>
 </label>
 </div>
 ${this._error ? html`<div class="error">${this._error}</div>` : ''}
 <button class="primary" @click=${this._createTip} ?disabled=${this._loading}>${this._loading ? 'Please wait…' : 'Tip'}</button>
 </div>

 <breez-payment-modal
 .open=${this._modalOpen}
 .title=${this.title}
 .description=${this.description}
 .invoice=${this._bolt11}
 .paymentHash=${this._paymentHash}
 @close=${() => this._modalOpen = false}
 ></breez-payment-modal>
 `;
 }

 static styles = css`
 :host { display: block; }
 .tip-jar { display:flex; flex-direction: column; gap:0.75rem; }
 .row { display:flex; flex-wrap: wrap; gap:0.5rem; align-items: center; }
 .amount-btn { background:#f1f1f1; border:1px solid #ddd; border-radius:6px; padding:0.4rem0.7rem; cursor:pointer; }
 .amount-btn[data-selected="true"], .amount-btn[data-selected] { background:#f89c1c; color: white; border-color:#e68a0a; }
 .custom-input { display:flex; align-items: center; gap:0.3rem; }
 .custom-input input { width:120px; padding:0.4rem; border:1px solid #ddd; border-radius:6px; }
 .primary { background:#f89c1c; border:0; color: white; padding:0.6rem1rem; border-radius:6px; cursor:pointer; align-self: start; }
 .primary:hover { background:#e68a0a; }
 .error { color:#b00020; }
 `;
}

declare global {
 interface HTMLElementTagNameMap {
 'breez-tip-jar': BreezTipJarElement;
 }
}
