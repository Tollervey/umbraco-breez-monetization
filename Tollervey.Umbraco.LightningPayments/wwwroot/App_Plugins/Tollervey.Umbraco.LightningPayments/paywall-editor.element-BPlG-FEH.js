import { LitElement as p, html as h, css as d, property as m, state as n, customElement as c } from "@umbraco-cms/backoffice/external/lit";
var f = Object.defineProperty, _ = Object.getOwnPropertyDescriptor, a = (e, t, r, o) => {
  for (var i = o > 1 ? void 0 : o ? _(t, r) : t, s = e.length - 1, u; s >= 0; s--)
    (u = e[s]) && (i = (o ? u(t, r, i) : u(i)) || i);
  return o && i && f(t, r, i), i;
};
let l = class extends p {
  constructor() {
    super(...arguments), this.modelValue = "", this._enabled = !1, this._fee = 0, this._error = "";
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
      this._enabled = !!t?.enabled;
      const r = Number(t?.fee ?? 0);
      this._fee = Number.isFinite(r) && r >= 0 ? r : 0, this._error = "";
    } catch {
      this._enabled = !1, this._fee = 0, this._error = "Invalid value";
    }
  }
  _emitChange() {
    const e = JSON.stringify({ enabled: this._enabled, fee: this._fee });
    this.dispatchEvent(new CustomEvent("umbPropertyValueChange", { detail: { value: e } }));
  }
  render() {
    return h`
 <div class="editor">
 <uui-form-layout-item>
 <uui-toggle
 .checked=${this._enabled}
 @change=${(e) => {
      this._enabled = !!e.target.checked, this._emitChange();
    }}
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
 @input=${(e) => {
      const t = parseInt(e.target.value || "0", 10);
      this._fee = Number.isFinite(t) && t >= 0 ? t : 0, this._emitChange();
    }}
 ></uui-input>
 <uui-helper>Amount the user must pay to unlock the content.</uui-helper>
 </uui-form-layout-item>

 ${this._error ? h`<uui-alert color="danger">${this._error}</uui-alert>` : ""}
 </div>
 `;
  }
};
l.styles = [
  d`
 :host { display: block; }
 .editor { display: grid; gap: var(--uui-size-space-4); }
 `
];
a([
  m({ type: Object })
], l.prototype, "modelValue", 2);
a([
  n()
], l.prototype, "_enabled", 2);
a([
  n()
], l.prototype, "_fee", 2);
a([
  n()
], l.prototype, "_error", 2);
l = a([
  c("tollervey-paywall-editor")
], l);
export {
  l as TollerveyPaywallEditorElement
};
//# sourceMappingURL=paywall-editor.element-BPlG-FEH.js.map
