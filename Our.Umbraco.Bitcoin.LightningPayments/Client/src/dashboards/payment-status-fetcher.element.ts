import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, customElement, state } from '@umbraco-cms/backoffice/external/lit';

@customElement('payment-status-fetcher')
export class PaymentStatusFetcherElement extends UmbLitElement {
    static styles = css`
        :host {
            display: block;
        }
        #status-label {
            padding: 1rem;
            background: #f8f8f8;
            border-radius: var(--uui-border-radius);
            font-family: monospace;
        }
    `;

    @state()
    private _statusMessage = "Click the button to fetch payment status...";

    @state()
    private _isLoading = false;

    render() {
        return html`
            <div id="status-label">
                <strong>${this._statusMessage}</strong>
            </div>
            <uui-button
                look="primary"
                label="Fetch Status"
                @click=${this._handleFetchStatusClick}
                ?disabled=${this._isLoading}
                state=${this._isLoading ? 'waiting' : 'default'}>
            </uui-button>
        `;
    }

    private async _handleFetchStatusClick() {
        this._isLoading = true;

        /*const request = umbHttpClient.get({
            url: '/umbraco/management/api/v1/lightning-payments/status'
        });*/

        /*const { data, error } = await tryExecute(this, request);

        if (error) {
            console.error('Error fetching status:', error);
            this._statusMessage = "Error fetching status. See console for details.";
        } else {
            this._statusMessage = "Hello";
        }*/
        this._statusMessage = "Hello";
        this._isLoading = false;
    }
}

export default PaymentStatusFetcherElement;

declare global {
    interface HTMLElementTagNameMap {
        'payment-status-fetcher': PaymentStatusFetcherElement;
    }
}