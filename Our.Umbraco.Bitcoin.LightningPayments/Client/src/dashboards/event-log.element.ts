import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, customElement, state } from '@umbraco-cms/backoffice/external/lit';

@customElement('event-log')
export class EventLogElement extends UmbLitElement {
    @state()
    private eventLog: Array<{ time: string; type: string; details: string }> = [];

    private _evtSrc?: EventSource;

    connectedCallback(): void {
        super.connectedCallback();
        this.connectRealtime();
    }

    disconnectedCallback(): void {
        super.disconnectedCallback();
        this._evtSrc?.close();
        this._evtSrc = undefined;
    }

    private connectRealtime() {
        try {
            this._evtSrc?.close();
            this._evtSrc = new EventSource('/api/public/lightning/realtime/subscribe');
            this._evtSrc.addEventListener('open', () => console.log('SSE open'));
            this._evtSrc.onerror = (e: any) => console.error('SSE error', e);
            this._evtSrc.addEventListener('payment-succeeded', () => {
                // Could emit event to parent to refresh payments
            });
            this._evtSrc.addEventListener('breez-event', (ev: MessageEvent) => {
                try {
                    const data = JSON.parse(ev.data);
                    const entry = {
                        time: new Date().toLocaleTimeString(),
                        type: String(data?.type ?? 'Unknown'),
                        details: String(data?.details ?? '')
                    };
                    const max = 50;
                    this.eventLog = [entry, ...this.eventLog].slice(0, max);
                } catch (err) {
                    console.error('SSE breez-event parse error', err);
                }
            });
        } catch (err) {
            console.error('connectRealtime failed', err);
        }
    }

    render() {
        return html`
            <uui-box headline="Live events">
                <ul class="event-log">
                    ${this.eventLog.map(e => html`<li><span class="t">${e.time}</span> <strong>${e.type}</strong> <span class="d">${e.details}</span></li>`)}
                </ul>
            </uui-box>
        `;
    }

    static styles = css`
        :host {
            display: block;
        }
        .event-log {
            list-style: none;
            padding: 0;
            margin: 0;
            display: flex;
            flex-direction: column;
            gap: 0.25rem;
        }
        .event-log .t {
            color: var(--uui-color-text-alt);
            margin-right: 0.5rem;
        }
        .event-log .d {
            color: var(--uui-color-text-alt);
        }
    `;
}

export default EventLogElement;

declare global {
    interface HTMLElementTagNameMap {
        'event-log': EventLogElement;
    }
}