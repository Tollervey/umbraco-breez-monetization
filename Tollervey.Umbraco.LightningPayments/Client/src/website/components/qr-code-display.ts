import { LitElement, html, css } from 'lit';
import { customElement, property } from 'lit/decorators.js';
import * as QRCode from 'qrcode';

@customElement('breez-qr-code-display')
export class BreezQrCodeDisplay extends LitElement {
 @property({ type: String }) data: string = '';
 @property({ type: Number }) size: number =240;

 async updated(changed: Map<string, unknown>) {
 if (changed.has('data')) {
 await this._renderQr();
 }
 }

 private async _renderQr() {
 const canvas = this.renderRoot?.querySelector('canvas');
 if (!canvas || !this.data) return;
 await QRCode.toCanvas(canvas as HTMLCanvasElement, this.data, { width: this.size, margin:1 });
 }

 render() {
 return html`<canvas width="${this.size}" height="${this.size}"></canvas>`;
 }

 static styles = css`
 :host { display: inline-block; }
 canvas { background: white; border:1px solid #eee; border-radius:6px; }
 `;
}

declare global {
 interface HTMLElementTagNameMap {
 'breez-qr-code-display': BreezQrCodeDisplay;
 }
}
