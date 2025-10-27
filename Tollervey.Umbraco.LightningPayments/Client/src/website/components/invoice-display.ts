import { LitElement, html, css } from 'lit';
import { customElement, property } from 'lit/decorators.js';

@customElement('breez-invoice-display')
export class BreezInvoiceDisplay extends LitElement {
 @property({ type: String }) invoice: string = '';

 private _copyTimer: number | null = null;
 @property({ type: Boolean }) copied: boolean = false;

 disconnectedCallback(): void {
 super.disconnectedCallback();
 if (this._copyTimer) { clearTimeout(this._copyTimer); this._copyTimer = null; }
 }

 private async _copy() {
 await navigator.clipboard.writeText(this.invoice);
 this.copied = true;
 this._copyTimer = window.setTimeout(() => (this.copied = false),1500);
 }

 render() {
 const lightningUri = this.invoice ? `lightning:${this.invoice}` : '';
 return html`
 <div class="inv">
 <textarea readonly .value=${this.invoice}></textarea>
 <div class="row">
 <button class="secondary" @click=${this._copy}>${this.copied ? 'Copied' : 'Copy'}</button>
 ${lightningUri ? html`<a class="primary" href="${lightningUri}">Open in wallet</a>` : ''}
 </div>
 </div>`;
 }

 static styles = css`
 :host { display: block; }
 .inv { display:flex; flex-direction: column; gap: var(--lp-spacing); }
 textarea { width:100%; height:120px; border: var(--lp-border); border-radius: var(--lp-radius); padding:0.5rem; font-family: monospace; background: var(--lp-color-surface); color: var(--lp-color-text); }
 .row { display:flex; gap:0.5rem; align-items: center; }
 .secondary { background: transparent; border: var(--lp-border); border-radius: var(--lp-radius); padding:0.4rem 0.7rem; cursor:pointer; color: var(--lp-color-text); }
 .primary { text-decoration:none; background: var(--lp-color-primary); color:white; padding:0.45rem 0.75rem; border-radius: var(--lp-radius); }
 `;
}

declare global {
 interface HTMLElementTagNameMap {
 'breez-invoice-display': BreezInvoiceDisplay;
 }
}
