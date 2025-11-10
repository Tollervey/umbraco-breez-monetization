import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, customElement, state } from '@umbraco-cms/backoffice/external/lit';

interface Payment {
    paymentHash: string;
    contentId: number;
    userSessionId: string;
    status: string;
}

@customElement('payments-table')
export class PaymentsTableElement extends UmbLitElement {
    @state()
    private payments: Payment[] = [];

    @state()
    private filteredPayments: Payment[] = [];

    @state()
    private searchTerm: string = "";

    @state()
    private rowActionBusy: Record<string, boolean> = {};

    @state()
    private rowActionError: Record<string, string> = {};

    connectedCallback(): void {
        super.connectedCallback();
        this.loadPayments();
    }

    async loadPayments() {
        const url = "/umbraco/management/api/lightningpayments/GetAllPayments";
        try {
            const response = await fetch(url);
            if (response.ok) {
                this.payments = await response.json();
                this.filteredPayments = this.payments;
            }
        } catch (error) {
            console.error('loadPayments error', error);
        }
    }

    private handleSearch(e: Event) {
        const target = e.target as HTMLInputElement;
        this.searchTerm = target.value.toLowerCase();
        this.filteredPayments = this.payments.filter(
            (p) => p.paymentHash.toLowerCase().includes(this.searchTerm) ||
                   p.contentId.toString().includes(this.searchTerm) ||
                   p.status.toLowerCase().includes(this.searchTerm)
        );
    }

    private async sendRowAction(endpoint: string, paymentHash: string) {
        this.rowActionBusy = { ...this.rowActionBusy, [paymentHash]: true };
        this.rowActionError = { ...this.rowActionError, [paymentHash]: "" };
        try {
            const url = `/umbraco/management/api/lightningpayments/${endpoint}`;
            const res = await fetch(url, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ paymentHash })
            });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            await this.loadPayments();
        } catch (err: any) {
            this.rowActionError = { ...this.rowActionError, [paymentHash]: err?.message ?? "Action failed" };
        } finally {
            this.rowActionBusy = { ...this.rowActionBusy, [paymentHash]: false };
        }
    }

    render() {
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

    static styles = css`
        :host {
            display: block;
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
    `;
}

export default PaymentsTableElement;

declare global {
    interface HTMLElementTagNameMap {
        'payments-table': PaymentsTableElement;
    }
}