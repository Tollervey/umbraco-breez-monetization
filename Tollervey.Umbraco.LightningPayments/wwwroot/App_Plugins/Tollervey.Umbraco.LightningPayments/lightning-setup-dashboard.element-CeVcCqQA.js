import { UmbLitElement as u } from "@umbraco-cms/backoffice/lit-element";
import { html as l, css as g, state as n, customElement as h } from "@umbraco-cms/backoffice/external/lit";
var p = Object.defineProperty, m = Object.getOwnPropertyDescriptor, s = (e, t, i, r) => {
  for (var o = r > 1 ? void 0 : r ? m(t, i) : t, d = e.length - 1, c; d >= 0; d--)
    (c = e[d]) && (o = (r ? c(t, i, o) : c(o)) || o);
  return r && o && p(t, i, o), o;
};
let a = class extends u {
  constructor() {
    super(...arguments), this._loading = !0, this._saving = !1, this._savingFlags = !1, this._state = null, this._flags = null, this._apiKey = "", this._mnemonic = "", this._conn = "", this._network = "Mainnet";
  }
  connectedCallback() {
    super.connectedCallback(), this._load();
  }
  async _load() {
    this._loading = !0;
    try {
      const [e, t] = await Promise.all([
        fetch("/umbraco/management/api/LightningSetup/State", { credentials: "same-origin" }),
        fetch("/umbraco/management/api/LightningSetup/Runtime", { credentials: "same-origin" })
      ]);
      if (!e.ok) throw new Error("Failed to load state");
      const i = await e.json();
      if (this._state = i, this._conn = i.connectionString ?? "", this._network = i.network ?? "Mainnet", !t.ok) throw new Error("Failed to load runtime flags");
      const r = await t.json();
      this._flags = r;
    } catch (e) {
      console.error(e), this._state = null, this._flags = null;
    } finally {
      this._loading = !1;
    }
  }
  async _save(e) {
    if (e.preventDefault(), !!this._state?.canSaveDevSecrets) {
      if (!this._apiKey?.trim() || !this._mnemonic?.trim()) {
        window.UmbNotification?.open?.({
          color: "danger",
          headline: "Missing values",
          message: "Breez API key and Mnemonic are required."
        });
        return;
      }
      this._saving = !0;
      try {
        const t = await fetch("/umbraco/management/api/LightningSetup/SaveDevSecrets", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "same-origin",
          body: JSON.stringify({
            breezApiKey: this._apiKey.trim(),
            mnemonic: this._mnemonic.trim(),
            connectionString: this._conn?.trim() || void 0,
            network: this._network
          })
        }), i = await t.json();
        if (!t.ok) throw new Error(i?.message || "Failed to save");
        window.UmbNotification?.open?.({
          color: "positive",
          headline: "Saved",
          message: i.restartRequired ? "Secrets saved to User Secrets. Restart the app to apply." : "Secrets saved."
        }), this._apiKey = "", this._mnemonic = "", await this._load();
      } catch (t) {
        console.error(t), window.UmbNotification?.open?.({
          color: "danger",
          headline: "Error",
          message: t?.message ?? "Failed to save secrets."
        });
      } finally {
        this._saving = !1;
      }
    }
  }
  async _saveFlags(e) {
    this._savingFlags = !0;
    try {
      const t = await fetch("/umbraco/management/api/LightningSetup/Runtime", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "same-origin",
        body: JSON.stringify(e)
        // property names match JsonPropertyName in server model
      }), i = await t.json().catch(() => ({}));
      if (!t.ok) throw new Error(i?.message || "Failed to save runtime flags");
      window.UmbNotification?.open?.({
        color: "positive",
        headline: "Runtime flags saved",
        message: "Changes applied."
      }), this._flags = e;
    } catch (t) {
      console.error(t), window.UmbNotification?.open?.({
        color: "danger",
        headline: "Error",
        message: t?.message ?? "Failed to save runtime flags."
      }), await this._load();
    } finally {
      this._savingFlags = !1;
    }
  }
  _toggleFlag(e, t) {
    if (!this._flags) return;
    const i = { ...this._flags, [e]: t };
    this._saveFlags(i);
  }
  _renderProdHelp() {
    return l`
      <uui-box headline="Production configuration">
        <p class="muted">In Production, set secrets via environment variables (or a secrets store). Example:</p>
        <pre class="code">LightningPayments__BreezApiKey=your-api-key
LightningPayments__Mnemonic="word1 word2 ... word24"
LightningPayments__ConnectionString="Data Source=/var/www/site/App_Data/LightningPayments/payment.db"
LightningPayments__Network=Mainnet</pre>
        <p class="muted">Then restart the application and verify health at <code class="code">/health/ready</code>.</p>
      </uui-box>
    `;
  }
  _renderDevForm() {
    return l`
      <uui-box headline="Development setup">
        <form @submit=${this._save}>
          <div class="row">
            <uui-label for="api">Breez API key</uui-label>
            <uui-input id="api" .value=${this._apiKey} @input=${(e) => this._apiKey = e.target.value} required></uui-input>
          </div>

          <div class="row">
            <uui-label for="mn">Mnemonic (24 words)</uui-label>
            <uui-textarea id="mn" rows="3" .value=${this._mnemonic} @input=${(e) => this._mnemonic = e.target.value} required></uui-textarea>
          </div>

          <div class="row">
            <uui-label for="net">Network</uui-label>
            <select id="net" .value=${this._network} @change=${(e) => this._network = e.target.value}>
              <option value="Mainnet">Mainnet (live)</option>
              <option value="Testnet">Testnet (for testing)</option>
              <option value="Regtest">Regtest (local)</option>
            </select>
            <div class="muted">Switching network requires an app restart. Prefer a separate working directory per network.</div>
          </div>

          <div class="row">
            <uui-label for="cs">Connection string (optional)</uui-label>
            <uui-input id="cs" .value=${this._conn} @input=${(e) => this._conn = e.target.value}></uui-input>
            <div class="muted">Default: Data Source=~/App_Data/LightningPayments/payment.db</div>
          </div>

          <div class="row">
            <uui-button type="submit" look="primary" label="Save to User Secrets" ?disabled=${this._saving}>
              ${this._saving ? "Saving�" : "Save to User Secrets"}
            </uui-button>
          </div>
        </form>
      </uui-box>
    `;
  }
  _renderFlags() {
    return this._flags ? l`
      <uui-box headline="Runtime flags">
        <div class="flags">
          <div class="flag-row">
            <div>Enabled</div>
            <uui-toggle
              ?disabled=${this._savingFlags}
              .checked=${this._flags.enabled}
              @change=${(e) => this._toggleFlag("enabled", !!e.target.checked)}>
            </uui-toggle>
          </div>

          <div class="flag-row">
            <div>Hide UI when disabled</div>
            <uui-toggle
              ?disabled=${this._savingFlags}
              .checked=${this._flags.hideUiWhenDisabled}
              @change=${(e) => this._toggleFlag("hideUiWhenDisabled", !!e.target.checked)}>
            </uui-toggle>
          </div>

          <div class="flag-row">
            <div>Tip jar enabled</div>
            <uui-toggle
              ?disabled=${this._savingFlags}
              .checked=${this._flags.tipJarEnabled}
              @change=${(e) => this._toggleFlag("tipJarEnabled", !!e.target.checked)}>
            </uui-toggle>
          </div>

          <div class="flag-row">
            <div>Paywall enabled</div>
            <uui-toggle
              ?disabled=${this._savingFlags}
              .checked=${this._flags.paywallEnabled}
              @change=${(e) => this._toggleFlag("paywallEnabled", !!e.target.checked)}>
            </uui-toggle>
          </div>
        </div>
      </uui-box>
    ` : null;
  }
  _renderStatus() {
    if (!this._state) return null;
    const e = this._state.hasApiKey && this._state.hasMnemonic, t = !!(this._state.connectionString && this._state.connectionString.trim());
    return l`
      <uui-box headline="Status">
        <div class=${e ? "ok" : "warn"}>
          ${e ? "Secrets detected." : "Secrets missing. Add them below (Development only) or as environment variables (Production)."}
        </div>
        <div class="muted">Environment: ${this._state.environment}</div>
        <div class="muted">Network: ${this._state.network}</div>
        <div class="muted">Connection string: ${t ? "set" : "not set"}</div>
      </uui-box>
    `;
  }
  render() {
    return this._loading ? l`<div class="container"><uui-loader></uui-loader></div>` : l`
      <div class="container">
        <uui-box headline="Lightning Payments Setup">
          <div class="muted">Use this page to configure Lightning Payments quickly and safely.</div>
        </uui-box>

        ${this._renderStatus()}
        ${this._renderFlags()}

        ${this._state?.environment === "Development" && this._state?.canSaveDevSecrets ? this._renderDevForm() : this._renderProdHelp()}
      </div>
    `;
  }
};
a.styles = g`
    .container { max-width: 720px; }
    .row { margin: 0.5rem 0; }
    .muted { color: var(--uui-color-text-alt); }
    .code { font-family: var(--uui-font-family-monospace); background: var(--uui-color-surface-alt); padding: .5rem; border-radius: 4px; display:block; }
    .ok { color: var(--uui-color-positive); }
    .warn { color: var(--uui-color-warning); }
    .err { color: var(--uui-color-danger); }
    form uui-input, form uui-textarea, form select { width: 100%; }
    select { padding: .4rem; }
    .flags { display: grid; gap: .5rem; }
    .flag-row { display: flex; align-items: center; justify-content: space-between; }
  `;
s([
  n()
], a.prototype, "_loading", 2);
s([
  n()
], a.prototype, "_saving", 2);
s([
  n()
], a.prototype, "_savingFlags", 2);
s([
  n()
], a.prototype, "_state", 2);
s([
  n()
], a.prototype, "_flags", 2);
s([
  n()
], a.prototype, "_apiKey", 2);
s([
  n()
], a.prototype, "_mnemonic", 2);
s([
  n()
], a.prototype, "_conn", 2);
s([
  n()
], a.prototype, "_network", 2);
a = s([
  h("umb-lp-setup-dashboard")
], a);
export {
  a as UmbLpSetupDashboardElement
};
//# sourceMappingURL=lightning-setup-dashboard.element-CeVcCqQA.js.map
