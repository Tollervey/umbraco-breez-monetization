import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, customElement, property, state } from '@umbraco-cms/backoffice/external/lit';

@customElement('paywall-message-editor')
export class PaywallMessageEditorElement extends UmbLitElement {
    @property({ type: Object })
    modelValue: any = '';

    @state()
    private _value = '';

    connectedCallback() {
        super.connectedCallback();
        this._value = this.modelValue || '';
    }

    updated(changedProperties: Map<string | number | symbol, unknown>) {
        if (changedProperties.has('modelValue')) {
            this._value = this.modelValue || '';
        }
    }

    private _onInput(event: Event) {
        const target = event.target as HTMLTextAreaElement;
        this._value = target.value;
        this.dispatchEvent(new CustomEvent('umbPropertyValueChange', { detail: { value: this._value } }));
    }

    render() {
        return html`
            <uui-textarea
                .value=${this._value}
                @input=${this._onInput}
                label="Paywall Message"
                placeholder="Enter the paywall message here..."
                rows="5"
            ></uui-textarea>
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