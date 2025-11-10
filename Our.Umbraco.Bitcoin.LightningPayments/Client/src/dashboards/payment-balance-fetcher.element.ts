import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, customElement, state } from '@umbraco-cms/backoffice/external/lit';

@customElement('payment-balance-fetcher')
export class PaymentBalanceFetcherElement extends UmbLitElement {
    static styles = css`
        :host {
            display: block;
            margin-top: 1rem;
        }
        #balance-label {
            padding: 1rem;
            background: #e8f5e8;
            border-radius: var(--uui-border-radius);
            font-family: monospace;
        }
    `;

    @state()
    private _balanceMessage = "Click the button to fetch balance...";

    @state()
    private _isLoading = false;

    render() {
        return html`
            <div id="balance-label">
                <strong>${this._balanceMessage}</strong>
            </div>
            <uui-button
                look="secondary"
                label="Fetch Balance"
                @click=${this._handleFetchBalanceClick}
                ?disabled=${this._isLoading}
                state=${this._isLoading ? 'waiting' : 'default'}>
            </uui-button>
        `;
    }

    private async _handleFetchBalanceClick() {
        this._isLoading = true;

        /*const request = umbHttpClient.get({
            url: '/umbraco/management/api/v1/lightning-payments/balance'
        });*/

        /*const { data, error } = await tryExecute(this, request);

        if (error) {
            console.error('Error fetching balance:', error);
            this._balanceMessage = "Error fetching balance. See console for details.";
        } else {
            this._balanceMessage = "Balance: " + data.balance;
        }*/
        this._balanceMessage = "Balance: 1000 sats";
        this._isLoading = false;
    }
}

export default PaymentBalanceFetcherElement;

declare global {
    interface HTMLElementTagNameMap {
        'payment-balance-fetcher': PaymentBalanceFetcherElement;
    }
}