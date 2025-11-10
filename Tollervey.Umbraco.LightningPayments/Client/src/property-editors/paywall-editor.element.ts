import { LitElement, html, css } from '@umbraco-cms/backoffice/external/lit';
import { customElement, property, state } from '@umbraco-cms/backoffice/external/lit';

// A lightweight property editor UI for configuring the breezPaywall JSON string
// Stored value: JSON string with shape { enabled: boolean, fee: number }
@customElement('tollervey-paywall-editor')
export class TollerveyPaywallEditorElement extends LitElement {
 // The current value from Umbraco. We accept string (JSON) or object for robustness.
 @property({ type: Object }) modelValue: any = '';

 @state() private _enabled = false;
 @state() private _fee: number =0;
 @state() private _error = '';

 connectedCallback(): void {
 super.connectedCallback();
 this._fromModelValue(this.modelValue);
 }

 updated(changed: Map<string, unknown>) {
 if (changed.has('modelValue')) {
 this._fromModelValue(this.modelValue);
 }
 }

 private _fromModelValue(value: any) {
 try {
 let obj: any = value;
 if (typeof value === 'string' && value.trim() !== '') {
 try { obj = JSON.parse(value); } catch { /* ignore parse errors */ }
 }
 this._enabled = !!obj?.enabled;
 const fee = Number(obj?.fee ??0);
 this._fee = Number.isFinite(fee) && fee >=0 ? fee :0;
 this._error = '';
 } catch (e: any) {
 this._enabled = false;
 this._fee =0;
 this._error = 'Invalid value';
 }
 }

 private _emitChange() {
 const json = JSON.stringify({ enabled: this._enabled, fee: this._fee });
 this.dispatchEvent(new CustomEvent('umbPropertyValueChange', { detail: { value: json } }));
 }

 render() {
 return html`
 <div class="editor">
 <uui-form-layout-item>
 <uui-toggle
 .checked=${this._enabled}
 @change=${(e: any) => { this._enabled = !!e.target.checked; this._emitChange(); }}
 label="Enable paywall"
 ></uui-toggle>
 </uui-form-layout-item>

 <uui-form-layout-item>
 <uui-label for="fee">Fee (sats)</uui-label>
 <uui-input
 id="fee"
 type="number"
 min="0"
 step="1"
 .value=${String(this._fee)}
 @input=${(e: any) => { const v = parseInt(e.target.value || '0',10); this._fee = Number.isFinite(v) && v >=0 ? v :0; this._emitChange(); }}
 ></uui-input>
 <uui-helper>Amount the user must pay to unlock the content.</uui-helper>
 </uui-form-layout-item>

 ${this._error ? html`<uui-alert color="danger">${this._error}</uui-alert>` : ''}
 </div>
 `;
 }

 static styles = [
 css`
 :host { display: block; }
 .editor { display: grid; gap: var(--uui-size-space-4); }
 `,
 ];
}

declare global {
 interface HTMLElementTagNameMap {
 'tollervey-paywall-editor': TollerveyPaywallEditorElement;
 }
}
