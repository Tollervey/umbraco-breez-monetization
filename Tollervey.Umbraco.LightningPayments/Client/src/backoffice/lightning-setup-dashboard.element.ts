import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, customElement, state } from '@umbraco-cms/backoffice/external/lit';

type SetupState = {
  environment: 'Development' | 'Production' | 'Other';
  canSaveDevSecrets: boolean;
  hasApiKey: boolean;
  hasMnemonic: boolean;
  connectionString: string;
  network: 'Mainnet' | 'Testnet' | 'Regtest';
};

type RuntimeFlags = {
  enabled: boolean;
  hideUiWhenDisabled: boolean;
  tipJarEnabled: boolean;
  paywallEnabled: boolean;
};

@customElement('umb-lp-setup-dashboard')
export class UmbLpSetupDashboardElement extends UmbLitElement {
  static styles = css`
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

  @state() private _loading = true;
  @state() private _saving = false;
  @state() private _savingFlags = false;
  @state() private _state: SetupState | null = null;
  @state() private _flags: RuntimeFlags | null = null;
  @state() private _apiKey = '';
  @state() private _mnemonic = '';
  @state() private _conn = '';
  @state() private _network: SetupState['network'] = 'Mainnet';

  connectedCallback(): void {
    super.connectedCallback();
    this._load();
  }

  private async _load() {
    this._loading = true;
    try {
      // Load setup state and flags in parallel
      const [stateRes, flagsRes] = await Promise.all([
        fetch('/umbraco/management/api/LightningSetup/State', { credentials: 'same-origin' }),
        fetch('/umbraco/management/api/LightningSetup/Runtime', { credentials: 'same-origin' }),
      ]);

      if (!stateRes.ok) throw new Error('Failed to load state');
      const stateJson = (await stateRes.json()) as SetupState;
      this._state = stateJson;
      this._conn = stateJson.connectionString ?? '';
      this._network = stateJson.network ?? 'Mainnet';

      if (!flagsRes.ok) throw new Error('Failed to load runtime flags');
      const flagsJson = (await flagsRes.json()) as RuntimeFlags;
      this._flags = flagsJson;
    } catch (err) {
      console.error(err);
      this._state = null;
      this._flags = null;
    } finally {
      this._loading = false;
    }
  }

  private async _save(e: Event) {
    e.preventDefault();
    if (!this._state?.canSaveDevSecrets) return;
    if (!this._apiKey?.trim() || !this._mnemonic?.trim()) {
      (window as any).UmbNotification?.open?.({
        color: 'danger',
        headline: 'Missing values',
        message: 'Breez API key and Mnemonic are required.',
      });
      return;
    }

    this._saving = true;
    try {
      const res = await fetch('/umbraco/management/api/LightningSetup/SaveDevSecrets', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'same-origin',
        body: JSON.stringify({
          breezApiKey: this._apiKey.trim(),
          mnemonic: this._mnemonic.trim(),
          connectionString: this._conn?.trim() || undefined,
          network: this._network
        }),
      });
      const json = await res.json();
      if (!res.ok) throw new Error(json?.message || 'Failed to save');

      (window as any).UmbNotification?.open?.({
        color: 'positive',
        headline: 'Saved',
        message: json.restartRequired
          ? 'Secrets saved to User Secrets. Restart the app to apply.'
          : 'Secrets saved.',
      });

      this._apiKey = '';
      this._mnemonic = '';
      await this._load();
    } catch (err: any) {
      console.error(err);
      (window as any).UmbNotification?.open?.({
        color: 'danger',
        headline: 'Error',
        message: err?.message ?? 'Failed to save secrets.',
      });
    } finally {
      this._saving = false;
    }
  }

  private async _saveFlags(next: RuntimeFlags) {
    this._savingFlags = true;
    try {
      const res = await fetch('/umbraco/management/api/LightningSetup/Runtime', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'same-origin',
        body: JSON.stringify(next), // property names match JsonPropertyName in server model
      });
      const json = await res.json().catch(() => ({}));
      if (!res.ok) throw new Error(json?.message || 'Failed to save runtime flags');

      (window as any).UmbNotification?.open?.({
        color: 'positive',
        headline: 'Runtime flags saved',
        message: 'Changes applied.',
      });
      this._flags = next;
    } catch (err: any) {
      console.error(err);
      (window as any).UmbNotification?.open?.({
        color: 'danger',
        headline: 'Error',
        message: err?.message ?? 'Failed to save runtime flags.',
      });
      // reload to ensure UI matches server
      await this._load();
    } finally {
      this._savingFlags = false;
    }
  }

  private _toggleFlag<K extends keyof RuntimeFlags>(key: K, checked: boolean) {
    if (!this._flags) return;
    const next = { ...this._flags, [key]: checked } as RuntimeFlags;
    void this._saveFlags(next);
  }

  private _renderProdHelp() {
    return html`
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

  private _renderDevForm() {
    return html`
      <uui-box headline="Development setup">
        <form @submit=${this._save}>
          <div class="row">
            <uui-label for="api">Breez API key</uui-label>
            <uui-input id="api" .value=${this._apiKey} @input=${(e: any) => (this._apiKey = e.target.value)} required></uui-input>
          </div>

          <div class="row">
            <uui-label for="mn">Mnemonic (24 words)</uui-label>
            <uui-textarea id="mn" rows="3" .value=${this._mnemonic} @input=${(e: any) => (this._mnemonic = e.target.value)} required></uui-textarea>
          </div>

          <div class="row">
            <uui-label for="net">Network</uui-label>
            <select id="net" .value=${this._network} @change=${(e: any) => (this._network = (e.target.value as SetupState['network']))}>
              <option value="Mainnet">Mainnet (live)</option>
              <option value="Testnet">Testnet (for testing)</option>
              <option value="Regtest">Regtest (local)</option>
            </select>
            <div class="muted">Switching network requires an app restart. Prefer a separate working directory per network.</div>
          </div>

          <div class="row">
            <uui-label for="cs">Connection string (optional)</uui-label>
            <uui-input id="cs" .value=${this._conn} @input=${(e: any) => (this._conn = e.target.value)}></uui-input>
            <div class="muted">Default: Data Source=~/App_Data/LightningPayments/payment.db</div>
          </div>

          <div class="row">
            <uui-button type="submit" look="primary" label="Save to User Secrets" ?disabled=${this._saving}>
              ${this._saving ? 'Saving…' : 'Save to User Secrets'}
            </uui-button>
          </div>
        </form>
      </uui-box>
    `;
  }

  private _renderFlags() {
    if (!this._flags) return null;
    return html`
      <uui-box headline="Runtime flags">
        <div class="flags">
          <div class="flag-row">
            <div>Enabled</div>
            <uui-toggle
              ?disabled=${this._savingFlags}
              .checked=${this._flags.enabled}
              @change=${(e: any) => this._toggleFlag('enabled', !!e.target.checked)}>
            </uui-toggle>
          </div>

          <div class="flag-row">
            <div>Hide UI when disabled</div>
            <uui-toggle
              ?disabled=${this._savingFlags}
              .checked=${this._flags.hideUiWhenDisabled}
              @change=${(e: any) => this._toggleFlag('hideUiWhenDisabled', !!e.target.checked)}>
            </uui-toggle>
          </div>

          <div class="flag-row">
            <div>Tip jar enabled</div>
            <uui-toggle
              ?disabled=${this._savingFlags}
              .checked=${this._flags.tipJarEnabled}
              @change=${(e: any) => this._toggleFlag('tipJarEnabled', !!e.target.checked)}>
            </uui-toggle>
          </div>

          <div class="flag-row">
            <div>Paywall enabled</div>
            <uui-toggle
              ?disabled=${this._savingFlags}
              .checked=${this._flags.paywallEnabled}
              @change=${(e: any) => this._toggleFlag('paywallEnabled', !!e.target.checked)}>
            </uui-toggle>
          </div>
        </div>
      </uui-box>
    `;
  }

  private _renderStatus() {
    if (!this._state) return null;
    const ok = this._state.hasApiKey && this._state.hasMnemonic;
    const csSet = !!(this._state.connectionString && this._state.connectionString.trim());
    return html`
      <uui-box headline="Status">
        <div class=${ok ? 'ok' : 'warn'}>
          ${ok ? 'Secrets detected.' : 'Secrets missing. Add them below (Development only) or as environment variables (Production).'}
        </div>
        <div class="muted">Environment: ${this._state.environment}</div>
        <div class="muted">Network: ${this._state.network}</div>
        <div class="muted">Connection string: ${csSet ? 'set' : 'not set'}</div>
      </uui-box>
    `;
  }

  render() {
    if (this._loading) {
      return html`<div class="container"><uui-loader></uui-loader></div>`;
    }
    return html`
      <div class="container">
        <uui-box headline="Lightning Payments Setup">
          <div class="muted">Use this page to configure Lightning Payments quickly and safely.</div>
        </uui-box>

        ${this._renderStatus()}
        ${this._renderFlags()}

        ${this._state?.environment === 'Development' && this._state?.canSaveDevSecrets
          ? this._renderDevForm()
          : this._renderProdHelp()}
      </div>
    `;
  }
}

declare global {
  interface HTMLElementTagNameMap {
    'umb-lp-setup-dashboard': UmbLpSetupDashboardElement;
  }
}