import { LitElement as m, html as d, css as p, property as h, state as r, customElement as c } from "@umbraco-cms/backoffice/external/lit";
var b = Object.defineProperty, f = Object.getOwnPropertyDescriptor, u = (e, t, i, l) => {
  for (var a = l > 1 ? void 0 : l ? f(t, i) : t, o = e.length - 1, n; o >= 0; o--)
    (n = e[o]) && (a = (l ? n(t, i, a) : n(a)) || a);
  return l && a && b(t, i, a), a;
};
let s = class extends m {
  constructor() {
    super(...arguments), this.modelValue = "", this._enabled = !1, this._label = "Send a tip", this._defaultAmounts = [500, 1e3, 2500];
  }
  connectedCallback() {
    super.connectedCallback(), this._fromModelValue(this.modelValue);
  }
  updated(e) {
    e.has("modelValue") && this._fromModelValue(this.modelValue);
  }
  _fromModelValue(e) {
    try {
      let t = e;
      if (typeof e == "string" && e.trim() !== "")
        try {
          t = JSON.parse(e);
        } catch {
        }
      this._enabled = !!t?.enabled, this._label = typeof t?.label == "string" && t.label.trim() ? t.label : "Send a tip";
      const i = Array.isArray(t?.defaultAmounts) ? t.defaultAmounts.map((l) => parseInt(String(l), 10)).filter((l) => Number.isFinite(l) && l > 0) : [];
      this._defaultAmounts = i.length ? i : [500, 1e3, 2500];
    } catch {
      this._enabled = !1, this._label = "Send a tip", this._defaultAmounts = [500, 1e3, 2500];
    }
  }
  _emitChange() {
    const e = JSON.stringify({ enabled: this._enabled, label: this._label, defaultAmounts: this._defaultAmounts });
    this.dispatchEvent(new CustomEvent("umbPropertyValueChange", { detail: { value: e } }));
  }
  _onAddAmount() {
    const e = prompt("Add amount (sats):", "1000");
    if (!e) return;
    const t = parseInt(e, 10);
    !Number.isFinite(t) || t <= 0 || (this._defaultAmounts = [...this._defaultAmounts, t], this._emitChange());
  }
  _onRemoveAmount(e) {
    this._defaultAmounts = this._defaultAmounts.filter((t, i) => i !== e), this._emitChange();
  }
  render() {
    return d`
 <div class="editor">
 <uui-form-layout-item>
 <uui-toggle
 .checked=${this._enabled}
 @change=${(e) => {
      this._enabled = !!e.target.checked, this._emitChange();
    }}
 label="Enable tip jar"
 ></uui-toggle>
 </uui-form-layout-item>

 <uui-form-layout-item>
 <uui-label for="label">Label</uui-label>
 <uui-input id="label" type="text" .value=${this._label} @input=${(e) => {
      this._label = e.target.value, this._emitChange();
    }}></uui-input>
 <uui-helper>Shown next to the tip button.</uui-helper>
 </uui-form-layout-item>

 <uui-form-layout-item>
 <uui-label>Default amounts (sats)</uui-label>
 <div class="chips">
 ${this._defaultAmounts.map((e, t) => d`<span class="chip">${e}<button class="x" @click=${() => this._onRemoveAmount(t)} aria-label="Remove">�</button></span>`)}
 <uui-button look="secondary" @click=${this._onAddAmount}>Add</uui-button>
 </div>
 <uui-helper>Users can still enter a custom amount in the website component.</uui-helper>
 </uui-form-layout-item>
 </div>
 `;
  }
};
s.styles = [
  p`
 :host { display:block; }
 .editor { display:grid; gap: var(--uui-size-space-4); }
 .chips { display:flex; flex-wrap: wrap; gap:0.35rem; align-items: center; }
 .chip { padding:0.25rem0.5rem; border:1px solid #ddd; border-radius:999px; display:inline-flex; gap:0.35rem; align-items:center; }
 .x { appearance:none; border:0; background: none; cursor: pointer; font-size:1rem; line-height:1; }
 `
];
u([
  h({ type: Object })
], s.prototype, "modelValue", 2);
u([
  r()
], s.prototype, "_enabled", 2);
u([
  r()
], s.prototype, "_label", 2);
u([
  r()
], s.prototype, "_defaultAmounts", 2);
s = u([
  c("tollervey-tipjar-editor")
], s);
export {
  s as TollerveyTipJarEditorElement
};
//# sourceMappingURL=tipjar-editor.element-DVujOU2l.js.map
