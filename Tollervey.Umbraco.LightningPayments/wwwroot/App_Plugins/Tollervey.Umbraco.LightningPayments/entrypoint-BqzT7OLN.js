import { UMB_AUTH_CONTEXT as R } from "@umbraco-cms/backoffice/auth";
const L = {
  bodySerializer: (r) => JSON.stringify(
    r,
    (t, e) => typeof e == "bigint" ? e.toString() : e
  )
}, V = ({
  onSseError: r,
  onSseEvent: t,
  responseTransformer: e,
  responseValidator: o,
  sseDefaultRetryDelay: a,
  sseMaxRetryAttempts: l,
  sseMaxRetryDelay: c,
  sseSleepFn: n,
  url: i,
  ...f
}) => {
  let s;
  const h = n ?? ((j) => new Promise((u) => setTimeout(u, j)));
  return { stream: async function* () {
    let j = a ?? 3e3, u = 0;
    const b = f.signal ?? new AbortController().signal;
    for (; !b.aborted; ) {
      u++;
      const E = f.headers instanceof Headers ? f.headers : new Headers(f.headers);
      s !== void 0 && E.set("Last-Event-ID", s);
      try {
        const y = await fetch(i, { ...f, headers: E, signal: b });
        if (!y.ok)
          throw new Error(
            `SSE failed: ${y.status} ${y.statusText}`
          );
        if (!y.body) throw new Error("No body in SSE response");
        const g = y.body.pipeThrough(new TextDecoderStream()).getReader();
        let m = "";
        const d = () => {
          try {
            g.cancel();
          } catch {
          }
        };
        b.addEventListener("abort", d);
        try {
          for (; ; ) {
            const { done: w, value: W } = await g.read();
            if (w) break;
            m += W;
            const z = m.split(`

`);
            m = z.pop() ?? "";
            for (const v of z) {
              const B = v.split(`
`), A = [];
              let $;
              for (const p of B)
                if (p.startsWith("data:"))
                  A.push(p.replace(/^data:\s*/, ""));
                else if (p.startsWith("event:"))
                  $ = p.replace(/^event:\s*/, "");
                else if (p.startsWith("id:"))
                  s = p.replace(/^id:\s*/, "");
                else if (p.startsWith("retry:")) {
                  const k = Number.parseInt(
                    p.replace(/^retry:\s*/, ""),
                    10
                  );
                  Number.isNaN(k) || (j = k);
                }
              let x, I = !1;
              if (A.length) {
                const p = A.join(`
`);
                try {
                  x = JSON.parse(p), I = !0;
                } catch {
                  x = p;
                }
              }
              I && (o && await o(x), e && (x = await e(x))), t?.({
                data: x,
                event: $,
                id: s,
                retry: j
              }), A.length && (yield x);
            }
          }
        } finally {
          b.removeEventListener("abort", d), g.releaseLock();
        }
        break;
      } catch (y) {
        if (r?.(y), l !== void 0 && u >= l)
          break;
        const g = Math.min(
          j * 2 ** (u - 1),
          c ?? 3e4
        );
        await h(g);
      }
    }
  }() };
}, J = async (r, t) => {
  const e = typeof t == "function" ? await t(r) : t;
  if (e)
    return r.scheme === "bearer" ? `Bearer ${e}` : r.scheme === "basic" ? `Basic ${btoa(e)}` : e;
}, F = (r) => {
  switch (r) {
    case "label":
      return ".";
    case "matrix":
      return ";";
    case "simple":
      return ",";
    default:
      return "&";
  }
}, G = (r) => {
  switch (r) {
    case "form":
      return ",";
    case "pipeDelimited":
      return "|";
    case "spaceDelimited":
      return "%20";
    default:
      return ",";
  }
}, M = (r) => {
  switch (r) {
    case "label":
      return ".";
    case "matrix":
      return ";";
    case "simple":
      return ",";
    default:
      return "&";
  }
}, U = ({
  allowReserved: r,
  explode: t,
  name: e,
  style: o,
  value: a
}) => {
  if (!t) {
    const n = (r ? a : a.map((i) => encodeURIComponent(i))).join(G(o));
    switch (o) {
      case "label":
        return `.${n}`;
      case "matrix":
        return `;${e}=${n}`;
      case "simple":
        return n;
      default:
        return `${e}=${n}`;
    }
  }
  const l = F(o), c = a.map((n) => o === "label" || o === "simple" ? r ? n : encodeURIComponent(n) : O({
    allowReserved: r,
    name: e,
    value: n
  })).join(l);
  return o === "label" || o === "matrix" ? l + c : c;
}, O = ({
  allowReserved: r,
  name: t,
  value: e
}) => {
  if (e == null)
    return "";
  if (typeof e == "object")
    throw new Error(
      "Deeply-nested arrays/objects aren’t supported. Provide your own `querySerializer()` to handle these."
    );
  return `${t}=${r ? e : encodeURIComponent(e)}`;
}, N = ({
  allowReserved: r,
  explode: t,
  name: e,
  style: o,
  value: a,
  valueOnly: l
}) => {
  if (a instanceof Date)
    return l ? a.toISOString() : `${e}=${a.toISOString()}`;
  if (o !== "deepObject" && !t) {
    let i = [];
    Object.entries(a).forEach(([s, h]) => {
      i = [
        ...i,
        s,
        r ? h : encodeURIComponent(h)
      ];
    });
    const f = i.join(",");
    switch (o) {
      case "form":
        return `${e}=${f}`;
      case "label":
        return `.${f}`;
      case "matrix":
        return `;${e}=${f}`;
      default:
        return f;
    }
  }
  const c = M(o), n = Object.entries(a).map(
    ([i, f]) => O({
      allowReserved: r,
      name: o === "deepObject" ? `${e}[${i}]` : i,
      value: f
    })
  ).join(c);
  return o === "label" || o === "matrix" ? c + n : n;
}, Q = /\{[^{}]+\}/g, X = ({ path: r, url: t }) => {
  let e = t;
  const o = t.match(Q);
  if (o)
    for (const a of o) {
      let l = !1, c = a.substring(1, a.length - 1), n = "simple";
      c.endsWith("*") && (l = !0, c = c.substring(0, c.length - 1)), c.startsWith(".") ? (c = c.substring(1), n = "label") : c.startsWith(";") && (c = c.substring(1), n = "matrix");
      const i = r[c];
      if (i == null)
        continue;
      if (Array.isArray(i)) {
        e = e.replace(
          a,
          U({ explode: l, name: c, style: n, value: i })
        );
        continue;
      }
      if (typeof i == "object") {
        e = e.replace(
          a,
          N({
            explode: l,
            name: c,
            style: n,
            value: i,
            valueOnly: !0
          })
        );
        continue;
      }
      if (n === "matrix") {
        e = e.replace(
          a,
          `;${O({
            name: c,
            value: i
          })}`
        );
        continue;
      }
      const f = encodeURIComponent(
        n === "label" ? `.${i}` : i
      );
      e = e.replace(a, f);
    }
  return e;
}, K = ({
  baseUrl: r,
  path: t,
  query: e,
  querySerializer: o,
  url: a
}) => {
  const l = a.startsWith("/") ? a : `/${a}`;
  let c = (r ?? "") + l;
  t && (c = X({ path: t, url: c }));
  let n = e ? o(e) : "";
  return n.startsWith("?") && (n = n.substring(1)), n && (c += `?${n}`), c;
}, P = ({
  allowReserved: r,
  array: t,
  object: e
} = {}) => (a) => {
  const l = [];
  if (a && typeof a == "object")
    for (const c in a) {
      const n = a[c];
      if (n != null)
        if (Array.isArray(n)) {
          const i = U({
            allowReserved: r,
            explode: !0,
            name: c,
            style: "form",
            value: n,
            ...t
          });
          i && l.push(i);
        } else if (typeof n == "object") {
          const i = N({
            allowReserved: r,
            explode: !0,
            name: c,
            style: "deepObject",
            value: n,
            ...e
          });
          i && l.push(i);
        } else {
          const i = O({
            allowReserved: r,
            name: c,
            value: n
          });
          i && l.push(i);
        }
    }
  return l.join("&");
}, Y = (r) => {
  if (!r)
    return "stream";
  const t = r.split(";")[0]?.trim();
  if (t) {
    if (t.startsWith("application/json") || t.endsWith("+json"))
      return "json";
    if (t === "multipart/form-data")
      return "formData";
    if (["application/", "audio/", "image/", "video/"].some(
      (e) => t.startsWith(e)
    ))
      return "blob";
    if (t.startsWith("text/"))
      return "text";
  }
}, Z = (r, t) => t ? !!(r.headers.has(t) || r.query?.[t] || r.headers.get("Cookie")?.includes(`${t}=`)) : !1, ee = async ({
  security: r,
  ...t
}) => {
  for (const e of r) {
    if (Z(t, e.name))
      continue;
    const o = await J(e, t.auth);
    if (!o)
      continue;
    const a = e.name ?? "Authorization";
    switch (e.in) {
      case "query":
        t.query || (t.query = {}), t.query[a] = o;
        break;
      case "cookie":
        t.headers.append("Cookie", `${a}=${o}`);
        break;
      case "header":
      default:
        t.headers.set(a, o);
        break;
    }
  }
}, _ = (r) => K({
  baseUrl: r.baseUrl,
  path: r.path,
  query: r.query,
  querySerializer: typeof r.querySerializer == "function" ? r.querySerializer : P(r.querySerializer),
  url: r.url
}), q = (r, t) => {
  const e = { ...r, ...t };
  return e.baseUrl?.endsWith("/") && (e.baseUrl = e.baseUrl.substring(0, e.baseUrl.length - 1)), e.headers = H(r.headers, t.headers), e;
}, H = (...r) => {
  const t = new Headers();
  for (const e of r) {
    if (!e || typeof e != "object")
      continue;
    const o = e instanceof Headers ? e.entries() : Object.entries(e);
    for (const [a, l] of o)
      if (l === null)
        t.delete(a);
      else if (Array.isArray(l))
        for (const c of l)
          t.append(a, c);
      else l !== void 0 && t.set(
        a,
        typeof l == "object" ? JSON.stringify(l) : l
      );
  }
  return t;
};
class T {
  constructor() {
    this._fns = [];
  }
  clear() {
    this._fns = [];
  }
  getInterceptorIndex(t) {
    return typeof t == "number" ? this._fns[t] ? t : -1 : this._fns.indexOf(t);
  }
  exists(t) {
    const e = this.getInterceptorIndex(t);
    return !!this._fns[e];
  }
  eject(t) {
    const e = this.getInterceptorIndex(t);
    this._fns[e] && (this._fns[e] = null);
  }
  update(t, e) {
    const o = this.getInterceptorIndex(t);
    return this._fns[o] ? (this._fns[o] = e, t) : !1;
  }
  use(t) {
    return this._fns = [...this._fns, t], this._fns.length - 1;
  }
}
const te = () => ({
  error: new T(),
  request: new T(),
  response: new T()
}), re = P({
  allowReserved: !1,
  array: {
    explode: !0,
    style: "form"
  },
  object: {
    explode: !0,
    style: "deepObject"
  }
}), se = {
  "Content-Type": "application/json"
}, D = (r = {}) => ({
  ...L,
  headers: se,
  parseAs: "auto",
  querySerializer: re,
  ...r
}), ne = (r = {}) => {
  let t = q(D(), r);
  const e = () => ({ ...t }), o = (f) => (t = q(t, f), e()), a = te(), l = async (f) => {
    const s = {
      ...t,
      ...f,
      fetch: f.fetch ?? t.fetch ?? globalThis.fetch,
      headers: H(t.headers, f.headers),
      serializedBody: void 0
    };
    s.security && await ee({
      ...s,
      security: s.security
    }), s.requestValidator && await s.requestValidator(s), s.body && s.bodySerializer && (s.serializedBody = s.bodySerializer(s.body)), (s.serializedBody === void 0 || s.serializedBody === "") && s.headers.delete("Content-Type");
    const h = _(s);
    return { opts: s, url: h };
  }, c = async (f) => {
    const { opts: s, url: h } = await l(f), C = {
      redirect: "follow",
      ...s,
      body: s.serializedBody
    };
    let S = new Request(h, C);
    for (const d of a.request._fns)
      d && (S = await d(S, s));
    const j = s.fetch;
    let u = await j(S);
    for (const d of a.response._fns)
      d && (u = await d(u, S, s));
    const b = {
      request: S,
      response: u
    };
    if (u.ok) {
      if (u.status === 204 || u.headers.get("Content-Length") === "0")
        return s.responseStyle === "data" ? {} : {
          data: {},
          ...b
        };
      const d = (s.parseAs === "auto" ? Y(u.headers.get("Content-Type")) : s.parseAs) ?? "json";
      let w;
      switch (d) {
        case "arrayBuffer":
        case "blob":
        case "formData":
        case "json":
        case "text":
          w = await u[d]();
          break;
        case "stream":
          return s.responseStyle === "data" ? u.body : {
            data: u.body,
            ...b
          };
      }
      return d === "json" && (s.responseValidator && await s.responseValidator(w), s.responseTransformer && (w = await s.responseTransformer(w))), s.responseStyle === "data" ? w : {
        data: w,
        ...b
      };
    }
    const E = await u.text();
    let y;
    try {
      y = JSON.parse(E);
    } catch {
    }
    const g = y ?? E;
    let m = g;
    for (const d of a.error._fns)
      d && (m = await d(g, u, S, s));
    if (m = m || {}, s.throwOnError)
      throw m;
    return s.responseStyle === "data" ? void 0 : {
      error: m,
      ...b
    };
  }, n = (f) => (s) => c({ ...s, method: f }), i = (f) => async (s) => {
    const { opts: h, url: C } = await l(s);
    return V({
      ...h,
      body: h.body,
      headers: h.headers,
      method: f,
      url: C
    });
  };
  return {
    buildUrl: _,
    connect: n("CONNECT"),
    delete: n("DELETE"),
    get: n("GET"),
    getConfig: e,
    head: n("HEAD"),
    interceptors: a,
    options: n("OPTIONS"),
    patch: n("PATCH"),
    post: n("POST"),
    put: n("PUT"),
    request: c,
    setConfig: o,
    sse: {
      connect: i("CONNECT"),
      delete: i("DELETE"),
      get: i("GET"),
      head: i("HEAD"),
      options: i("OPTIONS"),
      patch: i("PATCH"),
      post: i("POST"),
      put: i("PUT"),
      trace: i("TRACE")
    },
    trace: n("TRACE")
  };
}, ae = ne(D({
  baseUrl: "https://localhost:44389"
})), ie = (r, t) => {
  console.log("Hello from my extension 🎉"), r.consumeContext(R, async (e) => {
    const o = e?.getOpenApiConfiguration();
    ae.setConfig({
      auth: o?.token ?? void 0,
      baseUrl: o?.base ?? "",
      credentials: o?.credentials ?? "same-origin"
    });
  });
}, ce = (r, t) => {
  console.log("Goodbye from my extension 👋");
};
export {
  ie as onInit,
  ce as onUnload
};
//# sourceMappingURL=entrypoint-BqzT7OLN.js.map
