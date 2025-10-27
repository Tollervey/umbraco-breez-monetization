import { LitElement, html, css } from 'lit';
import { customElement, property } from 'lit/decorators.js';
import * as QRCode from 'qrcode';

@customElement('breez-qr-code-display')
export class BreezQrCodeDisplay extends LitElement {
 @property({ type: String }) data: string = '';
 @property({ type: Number }) size: number =240;

 render() { return html`<canvas width="${this.size}" height="${this.size}"></canvas>`; }

 async updated(changed: Map<string, unknown>) { if (changed.has('data')) await this._renderQr(); }

 private async _renderQr() {
 const root = (this.shadowRoot as ShadowRoot | null);
 const canvas = root?.querySelector('canvas') as HTMLCanvasElement | null;
 if (!canvas || !this.data) return;
 await QRCode.toCanvas(canvas, this.data, { width: this.size, margin:1 });
 }

 static styles = css`
 :host { display: inline-block; }
 canvas {
 background: var(--lp-color-surface);
 border: var(--lp-border);
 border-radius: var(--lp-radius);
 box-shadow: var(--lp-shadow);
 }
 `;
}

declare global { interface HTMLElementTagNameMap { 'breez-qr-code-display': BreezQrCodeDisplay; } }
