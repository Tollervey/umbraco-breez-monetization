import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, customElement, state } from '@umbraco-cms/backoffice/external/lit';

@customElement('paywall-quote-fetcher')
export class PaywallQuoteFetcherElement extends UmbLitElement {
    @state()
    private quoteAmount = 1000;

    @state()
    private quoting = false;

    @state()
    private quoteError = "";

    @state()
    private quoteResult: { amountSat: number; feesSat: number; method: string } | null = null;

    private async getQuote() {
        this.quoting = true;
        this.quoteError = "";
        this.quoteResult = null;
        try {
            const url = new URL("/umbraco/management/api/lightningpayments/GetPaywallReceiveFeeQuote", location.origin);
            url.searchParams.set("contentId", String(0));
            const res = await fetch(url.toString());
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            this.quoteResult = await res.json();
        } catch (err: any) {
            this.quoteError = err?.message ?? "Failed to get quote";
        } finally {
            this.quoting = false;
        }
    }

    render() {
        return html`
            <uui-box headline="Receive fee quote">
                <div class="quote-row">
                    <uui-label for="q-amount">Amount (sats)</uui-label>
                    <uui-input
                        id="q-amount"
                        type="number"
                        min="1"
                        step="1"
                        .value=${String(this.quoteAmount)}
                        @input=${(e: any) => this.quoteAmount = Math.max(1, parseInt(e.target.value || 1, 10))}
                    ></uui-input>
                    <uui-button look="primary" @click=${this.getQuote} ?disabled=${this.quoting}>
                        ${this.quoting ? "Quoting..." : "Get quote"}
                    </uui-button>
                </div>
                ${this.quoteError ? html`<uui-alert color="danger">${this.quoteError}</uui-alert>` : ""}
                ${this.quoteResult ? html`<div class="quote-out">Estimated fees: <strong>${this.quoteResult.feesSat}</strong> sats (${this.quoteResult.method})</div>` : ""}
            </uui-box>
        `;
    }

    static styles = css`
        :host {
            display: block;
        }
        .quote-row {
            display: grid;
            grid-template-columns: 1fr 1fr auto;
            gap: 0.5rem;
            align-items: end;
        }
        .quote-out {
            margin-top: 0.5rem;
        }
    `;
}

export default PaywallQuoteFetcherElement;

declare global {
    interface HTMLElementTagNameMap {
        'paywall-quote-fetcher': PaywallQuoteFetcherElement;
    }
}