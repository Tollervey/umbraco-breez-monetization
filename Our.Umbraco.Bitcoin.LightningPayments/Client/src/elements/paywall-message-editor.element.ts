import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, customElement, property } from '@umbraco-cms/backoffice/external/lit';

@customElement('paywall-message-editor')
export class PaywallMessageEditorElement extends UmbLitElement {
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
        'paywall-message-editor': PaywallMessageEditorElement;
    }
}