import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, customElement } from '@umbraco-cms/backoffice/external/lit';
import './payment-status-fetcher.element.js';

@customElement('lightning-payments-view')
export class LightningPaymentsViewElement extends UmbLitElement {
    static styles = css`
        :host {
            display: block;
            padding: 20px;
        }
    `;

    render() {
        return html`
            <uui-box headline="Lightning Payments Dashboard">
                <payment-status-fetcher></payment-status-fetcher>
            </uui-box>
        `;
    }
}

export default LightningPaymentsViewElement;

declare global {
    interface HTMLElementTagNameMap {
        'lightning-payments-view': LightningPaymentsViewElement;
    }
}