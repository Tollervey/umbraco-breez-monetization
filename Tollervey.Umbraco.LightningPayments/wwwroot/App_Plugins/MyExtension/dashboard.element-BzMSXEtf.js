import { LitElement as d, html as h, css as c, state as o, customElement as m } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin as b } from "@umbraco-cms/backoffice/element-api";
var y = Object.defineProperty, p = Object.getOwnPropertyDescriptor, u = (e, s, t, i) => {
  for (var a = i > 1 ? void 0 : i ? p(s, t) : s, r = e.length - 1, n; r >= 0; r--)
    (n = e[r]) && (a = (i ? n(s, t, a) : n(a)) || a);
  return i && a && y(s, t, a), a;
};
let l = class extends b(d) {
  constructor() {
    super(), this.payments = [], this.filteredPayments = [], this.searchTerm = "", this.loadPayments();
  }
  async loadPayments() {
    try {
      const e = await fetch(
        "/umbraco/api/LightningPaymentsApi/GetAllPayments"
      );
      e.ok && (this.payments = await e.json(), this.filteredPayments = this.payments);
    } catch (e) {
      console.error("Failed to load payments:", e);
    }
  }
  handleSearch(e) {
    const s = e.target;
    this.searchTerm = s.value.toLowerCase(), this.filteredPayments = this.payments.filter(
      (t) => t.paymentHash.toLowerCase().includes(this.searchTerm) || t.contentId.toString().includes(this.searchTerm) || t.status.toLowerCase().includes(this.searchTerm)
    );
  }
  render() {
    return h`
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
      (e) => h`
                    <uui-table-row>
                      <uui-table-cell>${e.paymentHash}</uui-table-cell>
                      <uui-table-cell>${e.contentId}</uui-table-cell>
                      <uui-table-cell>${e.userSessionId}</uui-table-cell>
                      <uui-table-cell>${e.status}</uui-table-cell>
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
};
l.styles = [
  c`
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
    `
];
u([
  o()
], l.prototype, "payments", 2);
u([
  o()
], l.prototype, "filteredPayments", 2);
u([
  o()
], l.prototype, "searchTerm", 2);
l = u([
  m("lightning-payments-dashboard")
], l);
const f = l;
export {
  l as LightningPaymentsDashboardElement,
  f as default
};
//# sourceMappingURL=dashboard.element-BzMSXEtf.js.map
