import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, customElement, state } from '@umbraco-cms/backoffice/external/lit';
import * as QRCode from "qrcode";

@customElement('test-invoice-creator')
export class TestInvoiceCreatorElement extends UmbLitElement {
    @state()
    private testAmount = 1000;

    @state()
    private testDescription = "Test invoice";

    @state()
    private creatingInvoice = false;

    @state()
    private createdInvoice: string | null = null;

    @state()
    private createdPaymentHash: string | null = null;

    @state()
    private invoiceQrDataUrl: string | null = null;

    @state()
    private invoiceError = "";

    @state()
    private copyOk = false;

    private async createTestInvoice() {
        this.creatingInvoice = true;
        this.invoiceError = "";
        this.createdInvoice = null;
        this.createdPaymentHash = null;
        this.invoiceQrDataUrl = null;
        const url = "/umbraco/management/api/lightningpayments/CreateTestInvoice";
        try {
            const res = await fetch(url, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ amountSat: this.testAmount, description: this.testDescription })
            });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const data = await res.json();
            this.createdInvoice = data.invoice;
            this.createdPaymentHash = data.paymentHash;
            if (this.createdInvoice) {
                try {
                    this.invoiceQrDataUrl = await QRCode.toDataURL(this.createdInvoice, { width: 240, margin: 1 });
                } catch (qrErr) {
                    console.error('QR generation failed', qrErr);
                }
            }
        } catch (err: any) {
            this.invoiceError = err?.message ?? "Failed to create invoice";
        } finally {
            this.creatingInvoice = false;
        }
    }

    private async copyInvoice() {
        if (!this.createdInvoice) return;
        try {
            await navigator.clipboard.writeText(this.createdInvoice);
            this.copyOk = true;
            setTimeout(() => (this.copyOk = false), 1500);
        } catch (err) {
            console.error('copy failed', err);
        }
    }

    render() {
        const lightningUri = this.createdInvoice ? `lightning:${this.createdInvoice}` : null;
        return html`
            <uui-box headline="Test invoice">
                <div class="test-grid">
                    <div>
                        <uui-label for="ti-amount">Amount (sats)</uui-label>
                        <uui-input
                            id="ti-amount"
                            type="number"
                            min="1"
                            step="1"
                            .value=${String(this.testAmount)}
                            @input=${(e: any) => this.testAmount = Math.max(1, parseInt(e.target.value || 1, 10))}
                        ></uui-input>
                    </div>
                    <div>
                        <uui-label for="ti-desc">Description</uui-label>
                        <uui-input
                            id="ti-desc"
                            type="text"
                            .value=${this.testDescription}
                            @input=${(e: any) => this.testDescription = e.target.value}
                        ></uui-input>
                    </div>
                </div>
                ${this.invoiceError ? html`<uui-alert color="danger">${this.invoiceError}</uui-alert>` : ""}
                <div class="test-actions">
                    <uui-button
                        look="primary"
                        @click=${this.createTestInvoice}
                        ?disabled=${this.creatingInvoice}
                    >
                        ${this.creatingInvoice ? "Creating..." : "Create invoice"}
                    </uui-button>
                </div>
                ${this.createdInvoice ? html`
                    <div class="invoice-out">
                        ${this.invoiceQrDataUrl ? html`<img class="qr" src="${this.invoiceQrDataUrl}" alt="Invoice QR" />` : ""}
                        <div class="invoice-text">
                            <uui-label>Invoice</uui-label>
                            <uui-textarea readonly .value=${this.createdInvoice}></uui-textarea>
                            <div class="actions">
                                <uui-button look="secondary" @click=${this.copyInvoice}>
                                    ${this.copyOk ? "Copied" : "Copy"}
                                </uui-button>
                                ${lightningUri ? html`<a class="open-wallet" href="${lightningUri}">Open in wallet</a>` : ""}
                            </div>
                            <div class="hash">Payment hash: ${this.createdPaymentHash?.slice(0, 12)}...</div>
                        </div>
                    </div>
                ` : ""}
            </uui-box>
        `;
    }

    static styles = css`
        :host {
            display: block;
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
    `;
}

export default TestInvoiceCreatorElement;

declare global {
    interface HTMLElementTagNameMap {
        'test-invoice-creator': TestInvoiceCreatorElement;
    }
}