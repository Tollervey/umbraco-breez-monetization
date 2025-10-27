import { LitElement, html, css } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';

@customElement('breez-invoice-display')
export class BreezInvoiceDisplay extends LitElement {
 @property({ type: String }) invoice = '';
 @state() private _copied = false;
 private _copyResetTimeoutId: number | null = null;
 private _inputId = `invoice-input-${Math.random().toString(36).slice(2)}`;
 private _helperId = `invoice-helper-${Math.random().toString(36).slice(2)}`;
 private _statusId = `invoice-status-${Math.random().toString(36).slice(2)}`;

 private async _copyToClipboard() {
 try {
 await navigator.clipboard.writeText(this.invoice);
 this._copied = true;
 if (this._copyResetTimeoutId) {
 clearTimeout(this._copyResetTimeoutId);
 this._copyResetTimeoutId = null;
 }
 this._copyResetTimeoutId = window.setTimeout(() => {
 this._copied = false;
 this._copyResetTimeoutId = null;
 },2000);
 } catch (error) {
 console.error('Failed to copy invoice:', error);
 }
 }

 disconnectedCallback(): void {
 super.disconnectedCallback();
 if (this._copyResetTimeoutId) {
 clearTimeout(this._copyResetTimeoutId);
 this._copyResetTimeoutId = null;
 }
 }

 render() {
 return html`
 <div class="invoice-container" role="group" aria-labelledby="${this._inputId}" aria-describedby="${this._helperId}">
 <label class="invoice-label" for="${this._inputId}">Lightning Invoice:</label>
 <div class="invoice-wrapper">
 <input id="${this._inputId}" type="text" class="invoice-input" readonly .value=${this.invoice} />
 <button class="copy-button" @click=${this._copyToClipboard} title="Copy to clipboard" aria-label="Copy invoice to clipboard" aria-describedby="${this._helperId}">
 ${this._copied ? '?' : '??'}
 </button>
 </div>
 <p id="${this._helperId}" class="visually-hidden">This text field contains your Lightning invoice. You can copy it to the clipboard.</p>
 <div id="${this._statusId}" class="copy-status" role="status" aria-live="polite">
 ${this._copied ? html`<p class="copy-success">Copied to clipboard!</p>` : ''}
 </div>
 </div>
 `;
 }

 static styles = css`
 :host { display: block; }
 .invoice-container { margin:1rem0; }
 .invoice-label { display:block; font-weight:500; margin-bottom:0.5rem; color:#333; }
 .invoice-wrapper { display:flex; gap:0.5rem; }
 .invoice-input { flex:1; padding:0.75rem; border:1px solid #ddd; border-radius:4px; font-family: monospace; font-size:0.75rem; background:#f5f5f5; }
 .copy-button { background:#f89c1c; color:white; border:none; padding:0.75rem1rem; border-radius:4px; cursor:pointer; font-size:1rem; min-width:50px; }
 .copy-button:hover { background:#e68a0a; }
 .copy-success { color:#388e3c; font-size:0.875rem; margin-top:0.5rem; text-align:center; }
 .visually-hidden { position:absolute !important; height:1px; width:1px; overflow:hidden; clip:rect(1px,1px,1px,1px); white-space:nowrap; clip-path: inset(50%); border:0; padding:0; margin:-1px; }
 `;
}

declare global {
 interface HTMLElementTagNameMap {
 'breez-invoice-display': BreezInvoiceDisplay;
 }
}
