import {
  LitElement,
  css,
  html,
  customElement,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import QRCode from "qrcode";

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

  // New: status/limits/fees
  @state() private connected = false;
  @state() private offlineMode = false;
  @state() private minSat: number | null = null;
  @state() private maxSat: number | null = null;
  @state() private recommendedFees: any = null;
  @state() private loadingStatus = true;
  @state() private errorStatus = "";

  // New: test invoice
  @state() private testAmount = 1000;
  @state() private testDescription = "Test invoice";
  @state() private creatingInvoice = false;
  @state() private createdInvoice: string | null = null;
  @state() private createdPaymentHash: string | null = null;
  @state() private invoiceQrDataUrl: string | null = null;
  @state() private invoiceError = "";

  constructor() {
    super();
    this.loadAll();
  }

  private async loadAll() {
    await Promise.all([
      this.loadStatus(),
      this.loadLimits(),
      this.loadRecommendedFees(),
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
      if (!res.ok) return; // connected might be false
      const data = await res.json();
      this.minSat = typeof data.minSat === "number" ? data.minSat : null;
      this.maxSat = typeof data.maxSat === "number" ? data.maxSat : null;
    } catch {
      // ignore
    }
  }

  private async loadRecommendedFees() {
    try {
      const res = await fetch(
        "/umbraco/management/api/lightningpayments/GetRecommendedFees"
      );
      if (!res.ok) return;
      this.recommendedFees = await res.json();
    } catch {
      // ignore
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
      (p) =>
        p.paymentHash.toLowerCase().includes(this.searchTerm) ||
        p.contentId.toString().includes(this.searchTerm) ||
        p.status.toLowerCase().includes(this.searchTerm)
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
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            amountSat: this.testAmount,
            description: this.testDescription,
          }),
        }
      );
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      this.createdInvoice = data.invoice;
      this.createdPaymentHash = data.paymentHash;
      if (this.createdInvoice) {
        try {
          this.invoiceQrDataUrl = await QRCode.toDataURL(this.createdInvoice, {
            width: 240,
            margin: 1,
          });
        } catch (qrErr) {
          console.error("QR generation failed", qrErr);
        }
      }
    } catch (err: any) {
      this.invoiceError = err?.message ?? "Failed to create invoice";
    } finally {
      this.creatingInvoice = false;
    }
  }

  render() {
    return html`
      <umb-body-layout header-transparent>
        <div slot="header">
          <h2>Lightning Payments</h2>
        </div>
        <div slot="main">
          ${this.renderStatus()}
          ${this.renderTestTools()}
          ${this.renderPaymentsTable()}
        </div>
      </umb-body-layout>
    `;
  }

  private renderStatus() {
    return html`
      <uui-box headline="Status" style="margin-bottom: var(--uui-size-layout-1)">
        ${this.loadingStatus
          ? html`<div>Loading status…</div>`
          : this.errorStatus
          ? html`<uui-alert color="danger">${this.errorStatus}</uui-alert>`
          : html`
              <div class="status-grid">
                <div><strong>SDK Connected:</strong> ${this.connected ? "Yes" : "No"}</div>
                <div><strong>Offline Mode:</strong> ${this.offlineMode ? "Yes" : "No"}</div>
                <div><strong>Lightning Limits:</strong> ${this.minSat != null && this.maxSat != null ? `${this.minSat} – ${this.maxSat} sats` : "Unknown"}</div>
              </div>
              ${this.recommendedFees
                ? html`
                    <details class="fees-details">
                      <summary>Recommended on-chain fees</summary>
                      <pre>${JSON.stringify(this.recommendedFees, null, 2)}</pre>
                    </details>
                  `
                : ""}
            `}
      </uui-box>
    `;
  }

  private renderTestTools() {
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
          </uui-table-head>
          <uui-table-body>
            ${this.filteredPayments.map(
              (payment) => html`
                <uui-table-row>
                  <uui-table-cell>${payment.paymentHash}</uui-table-cell>
                  <uui-table-cell>${payment.contentId}</uui-table-cell>
                  <uui-table-cell>${payment.userSessionId}</uui-table-cell>
                  <uui-table-cell>${payment.status}</uui-table-cell>
                </uui-table-row>
              `
            )}
          </uui-table-body>
        </uui-table>
      </uui-box>
    `;
  }

  static styles = [
    css`
      :host {
        display: block;
        padding: var(--uui-size-layout-1);
      }

      uui-box {
        margin-bottom: var(--uui-size-layout-1);
      }

      h2 {
        margin-top: 0;
      }

      .status-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
        gap: 0.5rem 1rem;
      }

      .fees-details {
        margin-top: 0.5rem;
      }

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

      .hash {
        margin-top: 0.5rem;
        color: var(--uui-color-text-alt);
        font-family: monospace;
      }
    `,
  ];
}

export default LightningPaymentsDashboardElement;

declare global {
  interface HTMLElementTagNameMap {
    "lightning-payments-dashboard": LightningPaymentsDashboardElement;
  }
}
