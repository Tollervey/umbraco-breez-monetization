export type FiatCode = 'USD' | 'EUR' | 'GBP';

/** Lightweight CoinGecko rates fetcher, memoized for60s to avoid rate limits. */
export class CoinGeckoExchangeRateService {
 private cache: Map<string, { t: number; v: number }> = new Map();
 private ttlMs =60_000;
 private api = 'https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=';

 async getBtcPrice(code: FiatCode): Promise<number> {
 const key = code.toLowerCase();
 const now = Date.now();
 const hit = this.cache.get(key);
 if (hit && now - hit.t < this.ttlMs) return hit.v;
 const r = await fetch(this.api + key);
 if (!r.ok) throw new Error('rate fetch failed');
 const j = await r.json();
 const v = Number(j?.bitcoin?.[key]);
 if (!Number.isFinite(v) || v <=0) throw new Error('invalid rate');
 this.cache.set(key, { t: now, v });
 return v;
 }

 /** sats from fiat */
 toSats(amountFiat: number, price: number): number {
 return Math.round((amountFiat / price) *100_000_000);
 }

 /** fiat from sats */
 toFiat(sats: number, price: number): number {
 return (sats /100_000_000) * price;
 }
}
