import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, customElement, state } from '@umbraco-cms/backoffice/external/lit';

@customElement('status-fetcher')
export class StatusFetcherElement extends UmbLitElement {
    @state()
    private connected = false;

    @state()
    private offlineMode = false;

    @state()
    private minSat: number | null = null;

    @state()
    private maxSat: number | null = null;

    @state()
    private recommendedFees: any = null;

    @state()
    private loadingStatus = true;

    @state()
    private errorStatus = "";

    @state()
    private health: { status: string; description?: string } | null = null;

    @state()
    private refreshing = false;

    connectedCallback(): void {
        super.connectedCallback();
        this.loadAll();
    }

    private async loadAll() {
        this.refreshing = true;
        try {
            await Promise.all([
                this.loadStatus(),
                this.loadLimits(),
                this.loadRecommendedFees(),
                this.loadHealth(),
            ]);
        } catch (err) {
            console.error('loadAll error', err);
        } finally {
            this.refreshing = false;
        }
    }

    private async loadStatus() {
        this.loadingStatus = true;
        this.errorStatus = "";
        try {
            const url = "/umbraco/management/api/lightningpayments/GetStatus";
            const res = await fetch(url);
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const data = await res.json();
            this.connected = !!data.connected;
            this.offlineMode = !!data.offlineMode;
        } catch (err: any) {
            this.errorStatus = err?.message ?? "Failed to load status";
        } finally {
            this.loadingStatus = false;
        }
    }

    private async loadLimits() {
        const url = "/umbraco/management/api/lightningpayments/GetLightningReceiveLimits";
        try {
            const res = await fetch(url);
            if (!res.ok) return;
            const data = await res.json();
            this.minSat = typeof data.minSat === "number" ? data.minSat : null;
            this.maxSat = typeof data.maxSat === "number" ? data.maxSat : null;
        } catch (err) {
            console.error('loadLimits error', err);
        }
    }

    private async loadRecommendedFees() {
        const url = "/umbraco/management/api/lightningpayments/GetRecommendedFees";
        try {
            const res = await fetch(url);
            if (!res.ok) return;
            this.recommendedFees = await res.json();
        } catch (err) {
            console.error('loadRecommendedFees error', err);
        }
    }

    private async loadHealth() {
        const url = "/health/ready";
        try {
            const res = await fetch(url);
            if (!res.ok) {
                this.health = { status: `HTTP ${res.status}` };
                return;
            }
            const txt = await res.text();
            this.health = { status: "Healthy", description: txt?.substring(0, 120) };
        } catch (e: any) {
            this.health = { status: "Unknown", description: e?.message };
        }
    }

    private onRefreshClick = () => this.loadAll();

    render() {
        return html`
            <uui-box headline="Status">
                <div class="toolbar">
                    <uui-button look="secondary" @click=${this.onRefreshClick} ?disabled=${this.refreshing}>
                        ${this.refreshing ? "Refreshing..." : "Refresh"}
                    </uui-button>
                </div>
                ${this.loadingStatus ? html`<div>Loading status...</div>` :
                  this.errorStatus ? html`<uui-alert color="danger">${this.errorStatus}</uui-alert>` :
                  html`
                    <div class="status-grid">
                        <div><strong>SDK Connected:</strong> ${this.connected ? "Yes" : "No"}</div>
                        <div><strong>Offline Mode:</strong> ${this.offlineMode ? "Yes" : "No"}</div>
                        <div><strong>Lightning Limits:</strong> ${this.minSat != null && this.maxSat != null ? `${this.minSat} - ${this.maxSat} sats` : "Unknown"}</div>
                        <div><strong>Health:</strong> ${this.health?.status ?? "Unknown"}</div>
                    </div>
                    ${this.recommendedFees ? html`<details class="fees-details"><summary>Recommended on-chain fees</summary><pre>${JSON.stringify(this.recommendedFees, null, 2)}</pre></details>` : ""}
                  `}
            </uui-box>
        `;
    }

    static styles = css`
        :host {
            display: block;
        }
        .toolbar {
            display: flex;
            gap: 0.5rem;
            align-items: center;
            margin-bottom: 0.5rem;
        }
        .status-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 0.5rem 1rem;
        }
        .fees-details {
            margin-top: 0.5rem;
        }
    `;
}

export default StatusFetcherElement;

declare global {
    interface HTMLElementTagNameMap {
        'status-fetcher': StatusFetcherElement;
    }
}