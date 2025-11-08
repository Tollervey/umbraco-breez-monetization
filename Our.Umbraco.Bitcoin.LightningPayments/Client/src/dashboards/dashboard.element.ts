import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, customElement, state } from '@umbraco-cms/backoffice/external/lit';
//import { tryExecute } from '@umbraco-cms/backoffice/resources';
//import { umbHttpClient } from '@umbraco-cms/backoffice/http-client';

@customElement('lightning-payments-view')
export class LightningPaymentsViewElement extends UmbLitElement {
    static styles = css`
        :host {
            display: block;
            padding: 20px;
        }
        uui-box {
            margin-bottom: 1rem;
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
            <uui-box headline="Lightning Payments Dashboard">
                <div id="status-label">
                    <strong>${this._statusMessage}</strong>
                </div>
            </uui-box>
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

export default LightningPaymentsViewElement;

declare global {
    interface HTMLElementTagNameMap {
        'lightning-payments-view': LightningPaymentsViewElement;
    }
}