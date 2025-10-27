import { LitElement, html, css } from '@umbraco-cms/backoffice/external/lit';
import { customElement, property, state } from '@umbraco-cms/backoffice/external/lit';

@customElement('tollervey-tipjar-editor')
export class TollerveyTipJarEditorElement extends LitElement {
 @property({ type: Object }) modelValue: any = '';
 @state() private _enabled = false;
 @state() private _label = 'Send a tip';
 @state() private _defaultAmounts: number[] = [500,1000,2500];

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
 this._label = typeof obj?.label === 'string' && obj.label.trim() ? obj.label : 'Send a tip';
 const arr = Array.isArray(obj?.defaultAmounts) ? obj.defaultAmounts.map((x: any) => parseInt(String(x),10)).filter((n: number) => Number.isFinite(n) && n >0) : [];
 this._defaultAmounts = arr.length ? arr : [500,1000,2500];
 } catch {
 this._enabled = false;
 this._label = 'Send a tip';
 this._defaultAmounts = [500,1000,2500];
 }
 }

 private _emitChange() {
 const json = JSON.stringify({ enabled: this._enabled, label: this._label, defaultAmounts: this._defaultAmounts });
 this.dispatchEvent(new CustomEvent('umbPropertyValueChange', { detail: { value: json } }));
 }

 private _onAddAmount() {
 const val = prompt('Add amount (sats):', '1000');
 if (!val) return;
 const n = parseInt(val,10);
 if (!Number.isFinite(n) || n <=0) return;
 this._defaultAmounts = [...this._defaultAmounts, n];
 this._emitChange();
 }

 private _onRemoveAmount(idx: number) {
 this._defaultAmounts = this._defaultAmounts.filter((_, i) => i !== idx);
 this._emitChange();
 }

 render() {
 return html`
 <div class="editor">
 <uui-form-layout-item>
 <uui-toggle
 .checked=${this._enabled}
 @change=${(e: any) => { this._enabled = !!e.target.checked; this._emitChange(); }}
 label="Enable tip jar"
 ></uui-toggle>
 </uui-form-layout-item>

 <uui-form-layout-item>
 <uui-label for="label">Label</uui-label>
 <uui-input id="label" type="text" .value=${this._label} @input=${(e: any) => { this._label = e.target.value; this._emitChange(); }}></uui-input>
 <uui-helper>Shown next to the tip button.</uui-helper>
 </uui-form-layout-item>

 <uui-form-layout-item>
 <uui-label>Default amounts (sats)</uui-label>
 <div class="chips">
 ${this._defaultAmounts.map((amt, idx) => html`<span class="chip">${amt}<button class="x" @click=${() => this._onRemoveAmount(idx)} aria-label="Remove">×</button></span>`) }
 <uui-button look="secondary" @click=${this._onAddAmount}>Add</uui-button>
 </div>
 <uui-helper>Users can still enter a custom amount in the website component.</uui-helper>
 </uui-form-layout-item>
 </div>
 `;
 }

 static styles = [
 css`
 :host { display:block; }
 .editor { display:grid; gap: var(--uui-size-space-4); }
 .chips { display:flex; flex-wrap: wrap; gap:0.35rem; align-items: center; }
 .chip { padding:0.25rem0.5rem; border:1px solid #ddd; border-radius:999px; display:inline-flex; gap:0.35rem; align-items:center; }
 .x { appearance:none; border:0; background: none; cursor: pointer; font-size:1rem; line-height:1; }
 `,
 ];
}

declare global {
 interface HTMLElementTagNameMap { 'tollervey-tipjar-editor': TollerveyTipJarEditorElement; }
}
