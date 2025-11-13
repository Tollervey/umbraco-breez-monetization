import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, customElement } from '@umbraco-cms/backoffice/external/lit';

@customElement('simple-poc-element')
export class SimplePocElement extends UmbLitElement {
    static styles = css`
        :host {
            display: block;
            padding: 1rem;
            border: 1px solid #ccc;
            border-radius: 4px;
            background: #f9f9f9;
        }
    `;

    render() {
        return html`
            <div>
                <h3>Hello from Our.Umbraco.Bitcoin.LightningPayments!</h3>
                <p>This is a proof of concept website control loaded from the NuGet package.</p>
                <p>Package Version: 1.0.69</p>
                <p>Current Time: ${new Date().toLocaleString()}</p>
            </div>
        `;
    }
}

export default SimplePocElement;

declare global {
    interface HTMLElementTagNameMap {
        'simple-poc-element': SimplePocElement;
    }
}