// Per V14 Great Leap: Lit Equivalent for backoffice property editors
import { html, css, LitElement } from 'lit';
import { customElement, property } from 'lit/decorators.js';

@customElement('our-umbraco-bitcoin-lightning-payments-paywall-message-editor')
export class PaywallMessageEditorElement extends LitElement {
    @property({ type: String })
    value?: string;

    private _onInput(event: Event) {
        const target = event.target as HTMLInputElement;
        this.value = target.value;
        this.dispatchEvent(new CustomEvent('umb:change', { detail: { value: this.value } }));
    }

    render() {
        return html`
            <uui-input
                .value=${this.value || ''}
                @input=${this._onInput}
                label="Paywall Message"
                placeholder="Enter the paywall message here..."
            ></uui-input>
        `;
    }

    static styles = css`
        :host {
            display: block;
        }
    `;
}

export default PaywallMessageEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        'our-umbraco-bitcoin-lightning-payments-paywall-message-editor': PaywallMessageEditorElement;
    }
}