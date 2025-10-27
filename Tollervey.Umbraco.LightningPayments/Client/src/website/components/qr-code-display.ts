import { LitElement, html, css } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import QRCode from 'qrcode';

@customElement('breez-qr-code-display')
export class BreezQRCodeDisplay extends LitElement {
 @property({ type: String })
 data = '';

 @state()
 private _qrCodeDataUrl = '';

 private _captionId = `qr-caption-${Math.random().toString(36).slice(2)}`;

 async updated(changedProperties: Map<string, any>) {
 if (changedProperties.has('data') && this.data) {
 try {
 this._qrCodeDataUrl = await QRCode.toDataURL(this.data, {
 width:300,
 margin:2,
 color: {
 dark: '#000000',
 light: '#FFFFFF'
 }
 });
 } catch (error) {
 console.error('Error generating QR code:', error);
 }
 }
 }

 render() {
 if (!this._qrCodeDataUrl) {
 return html`<div class="loading" role="status" aria-live="polite">Generating QR code...</div>`;
 }

 return html`
 <div class="qr-container" role="group" aria-label="Lightning payment QR code">
 <img 
 src="${this._qrCodeDataUrl}"
 role="img"
 alt="QR code for Lightning invoice"
 aria-describedby="${this._captionId}"
 class="qr-code"
 />
 <p id="${this._captionId}" class="qr-instruction">Scan with your Lightning wallet</p>
 </div>
 `;
 }

 static styles = css`
 :host { display: block; }
 .qr-container { display: flex; flex-direction: column; align-items: center; margin:1rem0; }
 .qr-code { max-width:100%; height: auto; border:2px solid #f89c1c; border-radius:8px; padding:0.5rem; background: white; }
 .qr-instruction { margin-top:0.5rem; color: #666; font-size:0.875rem; }
 .loading { text-align: center; color: #666; padding:2rem; }
 `;
}

declare global {
 interface HTMLElementTagNameMap {
 'breez-qr-code-display': BreezQRCodeDisplay;
 }
}
