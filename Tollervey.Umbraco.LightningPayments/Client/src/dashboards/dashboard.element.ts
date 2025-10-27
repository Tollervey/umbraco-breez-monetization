import {
  LitElement,
  css,
  html,
  customElement,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import * as QRCode from "qrcode";

interface Payment {
  paymentHash: string;
  contentId: number;
  userSessionId: string;
  status: string;
}

@customElement("lightning-payments-dashboard")
export class LightningPaymentsDashboardElement extends UmbElementMixin(LitElement) {
  @state() private payments: Payment[] = [];
  @state() private filteredPayments: Payment[] = [];
  @state() private searchTerm: string = "";

  // status/limits/fees
  @state() private connected = false;
  @state() private offlineMode = false;
  @state() private minSat: number | null = null;
  @state() private maxSat: number | null = null;
  @state() private recommendedFees: any = null;
  @state() private loadingStatus = true;
  @state() private errorStatus = "";

  // health check
  @state() private health: { status: string; description?: string } | null = null;

  // quote
  @state() private quoteAmount =1000;
  @state() private quoting = false;
  @state() private quoteError = "";
  @state() private quoteResult: { amountSat: number; feesSat: number; method: "string" } | null = null;

  // test invoice
  @state() private testAmount =1000;
  @state() private testDescription = "Test invoice";
  @state() private creatingInvoice = false;
  @state() private createdInvoice: string | null = null;
  @state() private createdPaymentHash: string | null = null;
  @state() private invoiceQrDataUrl: string | null = null;
  @state() private invoiceError = "";

  // refresh tools
  @state() private autoRefresh = false;
  private refreshTimer: number | null = null;
  private readonly refreshIntervalMs =10000;
  @state() private refreshing = false;

  // copy feedback
  @state() private copyOk = false;

  // row actions
  @state() private rowActionBusy: Record<string, boolean> = {};
  @state() private rowActionError: Record<string, string> = {};

  constructor() {
    super();
    this.loadAll();
  }

  connectedCallback(): void {
    super.connectedCallback();
    if (this.autoRefresh) this.startAutoRefresh();
  }
  disconnectedCallback(): void {
    super.disconnectedCallback();
    this.stopAutoRefresh();
  }

  private async loadAll() {
    await Promise.all([
      this.loadStatus(),
      this.loadLimits(),
      this.loadRecommendedFees(),
      this.loadHealth(),
      this.loadPayments(),
    ]);
  }

  private async loadStatus() {
    this.loadingStatus = true;
    this.errorStatus = "";
    try {
      const res = await fetch(
        "/umbraco/management/api/lightningpayments/GetStatus"
      );
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      this.connected = !!data.connected;
      this.offlineMode = !!data.offlineMode;
    } catch (err: any) {
      this.errorStatus = err?.message ?? "Failed to load status";
    } finally {
      this.loadingStatus = false;
    }
  }

  private async loadLimits() {
    try {
      const res = await fetch(
        "/umbraco/management/api/lightningpayments/GetLightningReceiveLimits"
      );
      if (!res.ok) return;
      const data = await res.json();
      this.minSat = typeof data.minSat === "number" ? data.minSat : null;
      this.maxSat = typeof data.maxSat === "number" ? data.maxSat : null;
    } catch {}
  }

  private async loadRecommendedFees() {
    try {
      const res = await fetch(
        "/umbraco/management/api/lightningpayments/GetRecommendedFees"
      );
      if (!res.ok) return;
      this.recommendedFees = await res.json();
    } catch {}
  }

  private async loadHealth() {
    try {
      const res = await fetch("/health/ready");
      if (!res.ok) { this.health = { status: `HTTP ${res.status}` }; return; }
      const txt = await res.text();
      // Minimal surface: treat any OK body as healthy.
      this.health = { status: "Healthy", description: txt?.substring(0,120) };
    } catch (e: any) {
      this.health = { status: "Unknown", description: e?.message };
    }
  }

  async loadPayments() {
    try {
      const response = await fetch(
        "/umbraco/management/api/lightningpayments/GetAllPayments"
      );
      if (response.ok) {
        this.payments = await response.json();
        this.filteredPayments = this.payments;
      }
    } catch (error) {
      console.error("Failed to load payments:", error);
    }
  }

  private handleSearch(e: Event) {
    const target = e.target as HTMLInputElement;
    this.searchTerm = target.value.toLowerCase();
    this.filteredPayments = this.payments.filter(
      (p) => p.paymentHash.toLowerCase().includes(this.searchTerm) || p.contentId.toString().includes(this.searchTerm) || p.status.toLowerCase().includes(this.searchTerm)
    );
  }

  private async createTestInvoice() {
    this.creatingInvoice = true;
    this.invoiceError = "";
    this.createdInvoice = null;
    this.createdPaymentHash = null;
    this.invoiceQrDataUrl = null;
    try {
      const res = await fetch(
        "/umbraco/management/api/lightningpayments/CreateTestInvoice",
        { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ amountSat: this.testAmount, description: this.testDescription }) }
      );
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      this.createdInvoice = data.invoice;
      this.createdPaymentHash = data.paymentHash;
      if (this.createdInvoice) {
        try { this.invoiceQrDataUrl = await QRCode.toDataURL(this.createdInvoice, { width:240, margin:1 }); } catch (qrErr) { console.error("QR generation failed", qrErr); }
      }
    } catch (err: any) {
      this.invoiceError = err?.message ?? "Failed to create invoice";
    } finally {
      this.creatingInvoice = false;
    }
  }

  private async getQuote() {
    this.quoting = true;
    this.quoteError = "";
    this.quoteResult = null;
    try {
      const url = new URL("/umbraco/management/api/lightningpayments/GetPaywallReceiveFeeQuote", location.origin);
      url.searchParams.set("contentId", String(0)); // contentId not used for generic quote; server will validate
      const res = await fetch(url.toString());
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      this.quoteResult = await res.json();
    } catch (err: any) {
      this.quoteError = err?.message ?? "Failed to get quote";
    } finally {
      this.quoting = false;
    }
  }

  private onRefreshClick = async () => { this.refreshing = true; try { await this.loadAll(); } finally { this.refreshing = false; } };
  private toggleAutoRefresh = (e: Event) => { const checked = (e.target as HTMLInputElement).checked; this.autoRefresh = checked; if (checked) this.startAutoRefresh(); else this.stopAutoRefresh(); };
  private startAutoRefresh() { this.stopAutoRefresh(); this.refreshTimer = window.setInterval(() => this.loadAll(), this.refreshIntervalMs); }
  private stopAutoRefresh() { if (this.refreshTimer) { clearInterval(this.refreshTimer); this.refreshTimer = null; } }
  private async copyInvoice() { if (!this.createdInvoice) return; try { await navigator.clipboard.writeText(this.createdInvoice); this.copyOk = true; setTimeout(() => (this.copyOk = false),1500); } catch (err) { console.warn("Copy failed", err); } }
  private async sendRowAction(endpoint: string, paymentHash: string) { this.rowActionBusy = { ...this.rowActionBusy, [paymentHash]: true }; this.rowActionError = { ...this.rowActionError, [paymentHash]: "" }; try { const res = await fetch(`/umbraco/management/api/lightningpayments/${endpoint}`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ paymentHash }) }); if (!res.ok) throw new Error(`HTTP ${res.status}`); await this.loadPayments(); } catch (err: any) { this.rowActionError = { ...this.rowActionError, [paymentHash]: err?.message ?? "Action failed" }; } finally { this.rowActionBusy = { ...this.rowActionBusy, [paymentHash]: false }; } }

  render() {
    return html`
      <umb-body-layout header-transparent>
        <div slot="header"><h2>Lightning Payments</h2></div>
        <div slot="main">
          ${this.renderStatus()}
          ${this.renderQuote()}
          ${this.renderTestTools()}
          ${this.renderPaymentsTable()}
        </div>
      </umb-body-layout>`;
  }

  private renderStatus() {
    return html`
      <uui-box headline="Status" style="margin-bottom: var(--uui-size-layout-1)">
        <div class="toolbar">
          <uui-button look="secondary" @click=${this.onRefreshClick} ?disabled=${this.refreshing}>${this.refreshing ? "Refreshing…" : "Refresh"}</uui-button>
          <label class="auto"><input type="checkbox" .checked=${this.autoRefresh} @change=${this.toggleAutoRefresh} /><span>Auto-refresh</span></label>
        </div>
        ${this.loadingStatus ? html`<div>Loading status…</div>` : this.errorStatus ? html`<uui-alert color="danger">${this.errorStatus}</uui-alert>` : html`
          <div class="status-grid">
            <div><strong>SDK Connected:</strong> ${this.connected ? "Yes" : "No"}</div>
            <div><strong>Offline Mode:</strong> ${this.offlineMode ? "Yes" : "No"}</div>
            <div><strong>Lightning Limits:</strong> ${this.minSat != null && this.maxSat != null ? `${this.minSat} – ${this.maxSat} sats` : "Unknown"}</div>
            <div><strong>Health:</strong> ${this.health?.status ?? "Unknown"}</div>
          </div>
          ${this.recommendedFees ? html`<details class="fees-details"><summary>Recommended on-chain fees</summary><pre>${JSON.stringify(this.recommendedFees, null,2)}</pre></details>` : ""}
        `}
      </uui-box>`;
  }

  private renderQuote() {
    return html`
      <uui-box headline="Receive fee quote" style="margin-bottom: var(--uui-size-layout-1)">
        <div class="quote-row">
          <uui-label for="q-amount">Amount (sats)</uui-label>
          <uui-input id="q-amount" type="number" min="1" step="1" .value=${String(this.quoteAmount)} @input=${(e: any) => this.quoteAmount = Math.max(1, parseInt(e.target.value ||1,10))}></uui-input>
          <uui-button look="primary" @click=${this.getQuote} ?disabled=${this.quoting}>${this.quoting ? "Quoting…" : "Get quote"}</uui-button>
        </div>
        ${this.quoteError ? html`<uui-alert color="danger">${this.quoteError}</uui-alert>` : ""}
        ${this.quoteResult ? html`<div class="quote-out">Estimated fees: <strong>${this.quoteResult.feesSat}</strong> sats (${this.quoteResult.method})</div>` : ""}
      </uui-box>`;
  }

  private renderTestTools() {
    const lightningUri = this.createdInvoice ? `lightning:${this.createdInvoice}` : null;
    return html`
      <uui-box headline="Test invoice" style="margin-bottom: var(--uui-size-layout-1)">
        <div class="test-grid">
          <div>
            <uui-label for="ti-amount">Amount (sats)</uui-label>
            <uui-input
              id="ti-amount"
              type="number"
              min="1"
              step="1"
              .value=${String(this.testAmount)}
              @input=${(e: any) =>
                (this.testAmount = Math.max(1, parseInt(e.target.value || 1, 10)))}
            ></uui-input>
          </div>
          <div>
            <uui-label for="ti-desc">Description</uui-label>
            <uui-input
              id="ti-desc"
              type="text"
              .value=${this.testDescription}
              @input=${(e: any) => (this.testDescription = e.target.value)}
            ></uui-input>
          </div>
        </div>
        ${this.invoiceError
          ? html`<uui-alert color="danger">${this.invoiceError}</uui-alert>`
          : ""}
        <div class="test-actions">
          <uui-button
            look="primary"
            @click=${this.createTestInvoice}
            ?disabled=${this.creatingInvoice}
          >${this.creatingInvoice ? "Creating…" : "Create invoice"}</uui-button>
        </div>
        ${this.createdInvoice
          ? html`
              <div class="invoice-out">
                ${this.invoiceQrDataUrl
                  ? html`<img class="qr" src="${this.invoiceQrDataUrl}" alt="Invoice QR" />`
                  : ""}
                <div class="invoice-text">
                  <uui-label>Invoice</uui-label>
                  <uui-textarea
                    readonly
                    .value=${this.createdInvoice}
                  ></uui-textarea>
                  <div class="actions">
                    <uui-button look="secondary" @click=${this.copyInvoice}>${this.copyOk ? "Copied" : "Copy"}</uui-button>
                    ${lightningUri
                      ? html`<a class="open-wallet" href="${lightningUri}">Open in wallet</a>`
                      : ""}
                  </div>
                  <div class="hash">Payment hash: ${this.createdPaymentHash?.slice(0, 12)}…</div>
                </div>
              </div>
            `
          : ""}
      </uui-box>
    `;
  }

  private renderPaymentsTable() {
    return html`
      <uui-box headline="Payments">
        <div slot="header">
          <input
            type="text"
            placeholder="Search payments..."
            @input=${this.handleSearch}
            style="width:100%; padding:8px; margin-bottom:16px;"
          />
        </div>
        <uui-table>
          <uui-table-head>
            <uui-table-head-cell>Payment Hash</uui-table-head-cell>
            <uui-table-head-cell>Content ID</uui-table-head-cell>
            <uui-table-head-cell>Session ID</uui-table-head-cell>
            <uui-table-head-cell>Status</uui-table-head-cell>
            <uui-table-head-cell style="width:260px">Actions</uui-table-head-cell>
          </uui-table-head>
          <uui-table-body>
            ${this.filteredPayments.map((payment) => {
              const busy = !!this.rowActionBusy[payment.paymentHash];
              const error = this.rowActionError[payment.paymentHash];
              return html`
                <uui-table-row>
                  <uui-table-cell>${payment.paymentHash}</uui-table-cell>
                  <uui-table-cell>${payment.contentId}</uui-table-cell>
                  <uui-table-cell>${payment.userSessionId}</uui-table-cell>
                  <uui-table-cell>${payment.status}</uui-table-cell>
                  <uui-table-cell>
                    <div class="row-actions">
                      <uui-button look="secondary" compact @click=${() => this.sendRowAction("ConfirmPayment", payment.paymentHash)} ?disabled=${busy}>Confirm</uui-button>
                      <uui-button look="warning" compact @click=${() => this.sendRowAction("MarkAsFailed", payment.paymentHash)} ?disabled=${busy}>Fail</uui-button>
                      <uui-button look="danger" compact @click=${() => this.sendRowAction("MarkAsExpired", payment.paymentHash)} ?disabled=${busy}>Expire</uui-button>
                      <uui-button look="secondary" compact @click=${() => this.sendRowAction("MarkAsRefundPending", payment.paymentHash)} ?disabled=${busy}>Refund pending</uui-button>
                      <uui-button look="positive" compact @click=${() => this.sendRowAction("MarkAsRefunded", payment.paymentHash)} ?disabled=${busy}>Refunded</uui-button>
                    </div>
                    ${error ? html`<div class="row-error">${error}</div>` : ""}
                  </uui-table-cell>
                </uui-table-row>`;
            })}
          </uui-table-body>
        </uui-table>
      </uui-box>
    `;
  }

  static styles = [
    css`
      :host { display:block; padding: var(--uui-size-layout-1); }
      uui-box { margin-bottom: var(--uui-size-layout-1); }
      h2 { margin-top: 0; }
      .toolbar { display:flex; gap:0.5rem; align-items:center; margin-bottom:0.5rem; }
      .auto { display:flex; gap:0.35rem; align-items:center; color: var(--uui-color-text); }
      .status-grid { display:grid; grid-template-columns: repeat(auto-fit, minmax(200px,1fr)); gap:0.5rem1rem; }
      .fees-details { margin-top:0.5rem; }
      .quote-row { display:grid; grid-template-columns:1fr1fr auto; gap:0.5rem; align-items:end; }
      .quote-out { margin-top:0.5rem; }
      .test-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        gap: 1rem;
        align-items: end;
      }
      .test-actions {
        margin-top: 0.5rem;
      }
      .invoice-out {
        margin-top: 1rem;
        display: grid;
        grid-template-columns: 240px 1fr;
        gap: 1rem;
      }
      .qr {
        width: 240px;
        height: 240px;
        background: white;
        border: 1px solid var(--uui-color-border);
        border-radius: 4px;
        padding: 4px;
      }
      .invoice-text uui-textarea {
        width: 100%;
        height: 120px;
      }
      .actions {
        display: flex;
        gap: 0.5rem;
        align-items: center;
        margin-top: 0.5rem;
      }
      .open-wallet {
        text-decoration: none;
        background: var(--uui-color-highlight);
        color: var(--uui-color-surface);
        padding: 0.4rem 0.6rem;
        border-radius: 4px;
      }
      .hash {
        margin-top: 0.5rem;
        color: var(--uui-color-text-alt);
        font-family: monospace;
      }
      .row-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.25rem;
      }
      .row-error {
        margin-top: 0.25rem;
        color: var(--uui-color-danger);
        font-size: 0.85rem;
      }
    `,
  ];
}

export default LightningPaymentsDashboardElement;

declare global { interface HTMLElementTagNameMap { "lightning-payments-dashboard": LightningPaymentsDashboardElement; } }
