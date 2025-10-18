import {
  LitElement,
  css,
  html,
  customElement,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";

interface Payment {
  paymentHash: string;
  contentId: number;
  userSessionId: string;
  status: string;
}

@customElement("lightning-payments-dashboard")
export class LightningPaymentsDashboardElement extends UmbElementMixin(LitElement) {
  @state()
  private payments: Payment[] = [];

  @state()
  private filteredPayments: Payment[] = [];

  @state()
  private searchTerm: string = "";

  constructor() {
    super();
    this.loadPayments();
  }

  async loadPayments() {
    try {
      const response = await fetch(
        "/umbraco/api/LightningPaymentsApi/GetAllPayments"
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
    this.filteredPayments = this.payments.filter((p) =>
      p.paymentHash.toLowerCase().includes(this.searchTerm) ||
      p.contentId.toString().includes(this.searchTerm) ||
      p.status.toLowerCase().includes(this.searchTerm)
    );
  }

  render() {
    return html`
      <umb-body-layout header-transparent>
        <div slot="header">
          <h2>Lightning Payments Dashboard</h2>
        </div>
        <div slot="main">
          <uui-box>
            <div slot="header">
              <input
                type="text"
                placeholder="Search payments..."
                @input=${this.handleSearch}
                style="width: 100%; padding: 8px; margin-bottom: 16px;"
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
        </div>
      </umb-body-layout>
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
    `,
  ];
}

export default LightningPaymentsDashboardElement;

declare global {
  interface HTMLElementTagNameMap {
    "lightning-payments-dashboard": LightningPaymentsDashboardElement;
  }
}
