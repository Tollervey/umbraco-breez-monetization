import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, customElement } from '@umbraco-cms/backoffice/external/lit';
import './payment-status-fetcher.element.js';
import './payment-balance-fetcher.element.js';
import './status-fetcher.element.js';
import './paywall-quote-fetcher.element.js';
import './test-invoice-creator.element.js';
import './payments-table.element.js';
import './event-log.element.js';

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
                <payment-balance-fetcher></payment-balance-fetcher>
                <status-fetcher></status-fetcher>
                <paywall-quote-fetcher></paywall-quote-fetcher>
                <test-invoice-creator></test-invoice-creator>
                <payments-table></payments-table>
                <event-log></event-log>
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