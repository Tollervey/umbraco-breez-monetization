import { LitElement as se, html as L, css as ae, state as N, customElement as ue } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin as le } from "@umbraco-cms/backoffice/element-api";
var O = {}, Z, St;
function ce() {
  return St || (St = 1, Z = function() {
    return typeof Promise == "function" && Promise.prototype && Promise.prototype.then;
  }), Z;
}
var X = {}, _ = {}, Bt;
function H() {
  if (Bt) return _;
  Bt = 1;
  let t;
  const n = [
    0,
    // Not used
    26,
    44,
    70,
    100,
    134,
    172,
    196,
    242,
    292,
    346,
    404,
    466,
    532,
    581,
    655,
    733,
    815,
    901,
    991,
    1085,
    1156,
    1258,
    1364,
    1474,
    1588,
    1706,
    1828,
    1921,
    2051,
    2185,
    2323,
    2465,
    2611,
    2761,
    2876,
    3034,
    3196,
    3362,
    3532,
    3706
  ];
  return _.getSymbolSize = function(r) {
    if (!r) throw new Error('"version" cannot be null or undefined');
    if (r < 1 || r > 40) throw new Error('"version" should be in range from 1 to 40');
    return r * 4 + 17;
  }, _.getSymbolTotalCodewords = function(r) {
    return n[r];
  }, _.getBCHDigit = function(o) {
    let r = 0;
    for (; o !== 0; )
      r++, o >>>= 1;
    return r;
  }, _.setToSJISFunction = function(r) {
    if (typeof r != "function")
      throw new Error('"toSJISFunc" is not a valid function.');
    t = r;
  }, _.isKanjiModeEnabled = function() {
    return typeof t < "u";
  }, _.toSJIS = function(r) {
    return t(r);
  }, _;
}
var tt = {}, Tt;
function vt() {
  return Tt || (Tt = 1, (function(t) {
    t.L = { bit: 1 }, t.M = { bit: 0 }, t.Q = { bit: 3 }, t.H = { bit: 2 };
    function n(o) {
      if (typeof o != "string")
        throw new Error("Param is not a string");
      switch (o.toLowerCase()) {
        case "l":
        case "low":
          return t.L;
        case "m":
        case "medium":
          return t.M;
        case "q":
        case "quartile":
          return t.Q;
        case "h":
        case "high":
          return t.H;
        default:
          throw new Error("Unknown EC Level: " + o);
      }
    }
    t.isValid = function(r) {
      return r && typeof r.bit < "u" && r.bit >= 0 && r.bit < 4;
    }, t.from = function(r, e) {
      if (t.isValid(r))
        return r;
      try {
        return n(r);
      } catch {
        return e;
      }
    };
  })(tt)), tt;
}
var et, It;
function de() {
  if (It) return et;
  It = 1;
  function t() {
    this.buffer = [], this.length = 0;
  }
  return t.prototype = {
    get: function(n) {
      const o = Math.floor(n / 8);
      return (this.buffer[o] >>> 7 - n % 8 & 1) === 1;
    },
    put: function(n, o) {
      for (let r = 0; r < o; r++)
        this.putBit((n >>> o - r - 1 & 1) === 1);
    },
    getLengthInBits: function() {
      return this.length;
    },
    putBit: function(n) {
      const o = Math.floor(this.length / 8);
      this.buffer.length <= o && this.buffer.push(0), n && (this.buffer[o] |= 128 >>> this.length % 8), this.length++;
    }
  }, et = t, et;
}
var nt, Pt;
function he() {
  if (Pt) return nt;
  Pt = 1;
  function t(n) {
    if (!n || n < 1)
      throw new Error("BitMatrix size must be defined and greater than 0");
    this.size = n, this.data = new Uint8Array(n * n), this.reservedBit = new Uint8Array(n * n);
  }
  return t.prototype.set = function(n, o, r, e) {
    const i = n * this.size + o;
    this.data[i] = r, e && (this.reservedBit[i] = !0);
  }, t.prototype.get = function(n, o) {
    return this.data[n * this.size + o];
  }, t.prototype.xor = function(n, o, r) {
    this.data[n * this.size + o] ^= r;
  }, t.prototype.isReserved = function(n, o) {
    return this.reservedBit[n * this.size + o];
  }, nt = t, nt;
}
var rt = {}, Mt;
function fe() {
  return Mt || (Mt = 1, (function(t) {
    const n = H().getSymbolSize;
    t.getRowColCoords = function(r) {
      if (r === 1) return [];
      const e = Math.floor(r / 7) + 2, i = n(r), s = i === 145 ? 26 : Math.ceil((i - 13) / (2 * e - 2)) * 2, u = [i - 7];
      for (let a = 1; a < e - 1; a++)
        u[a] = u[a - 1] - s;
      return u.push(6), u.reverse();
    }, t.getPositions = function(r) {
      const e = [], i = t.getRowColCoords(r), s = i.length;
      for (let u = 0; u < s; u++)
        for (let a = 0; a < s; a++)
          u === 0 && a === 0 || // top-left
          u === 0 && a === s - 1 || // bottom-left
          u === s - 1 && a === 0 || e.push([i[u], i[a]]);
      return e;
    };
  })(rt)), rt;
}
var it = {}, Nt;
function ge() {
  if (Nt) return it;
  Nt = 1;
  const t = H().getSymbolSize, n = 7;
  return it.getPositions = function(r) {
    const e = t(r);
    return [
      // top-left
      [0, 0],
      // top-right
      [e - n, 0],
      // bottom-left
      [0, e - n]
    ];
  }, it;
}
var ot = {}, qt;
function me() {
  return qt || (qt = 1, (function(t) {
    t.Patterns = {
      PATTERN000: 0,
      PATTERN001: 1,
      PATTERN010: 2,
      PATTERN011: 3,
      PATTERN100: 4,
      PATTERN101: 5,
      PATTERN110: 6,
      PATTERN111: 7
    };
    const n = {
      N1: 3,
      N2: 3,
      N3: 40,
      N4: 10
    };
    t.isValid = function(e) {
      return e != null && e !== "" && !isNaN(e) && e >= 0 && e <= 7;
    }, t.from = function(e) {
      return t.isValid(e) ? parseInt(e, 10) : void 0;
    }, t.getPenaltyN1 = function(e) {
      const i = e.size;
      let s = 0, u = 0, a = 0, l = null, h = null;
      for (let E = 0; E < i; E++) {
        u = a = 0, l = h = null;
        for (let g = 0; g < i; g++) {
          let c = e.get(E, g);
          c === l ? u++ : (u >= 5 && (s += n.N1 + (u - 5)), l = c, u = 1), c = e.get(g, E), c === h ? a++ : (a >= 5 && (s += n.N1 + (a - 5)), h = c, a = 1);
        }
        u >= 5 && (s += n.N1 + (u - 5)), a >= 5 && (s += n.N1 + (a - 5));
      }
      return s;
    }, t.getPenaltyN2 = function(e) {
      const i = e.size;
      let s = 0;
      for (let u = 0; u < i - 1; u++)
        for (let a = 0; a < i - 1; a++) {
          const l = e.get(u, a) + e.get(u, a + 1) + e.get(u + 1, a) + e.get(u + 1, a + 1);
          (l === 4 || l === 0) && s++;
        }
      return s * n.N2;
    }, t.getPenaltyN3 = function(e) {
      const i = e.size;
      let s = 0, u = 0, a = 0;
      for (let l = 0; l < i; l++) {
        u = a = 0;
        for (let h = 0; h < i; h++)
          u = u << 1 & 2047 | e.get(l, h), h >= 10 && (u === 1488 || u === 93) && s++, a = a << 1 & 2047 | e.get(h, l), h >= 10 && (a === 1488 || a === 93) && s++;
      }
      return s * n.N3;
    }, t.getPenaltyN4 = function(e) {
      let i = 0;
      const s = e.data.length;
      for (let a = 0; a < s; a++) i += e.data[a];
      return Math.abs(Math.ceil(i * 100 / s / 5) - 10) * n.N4;
    };
    function o(r, e, i) {
      switch (r) {
        case t.Patterns.PATTERN000:
          return (e + i) % 2 === 0;
        case t.Patterns.PATTERN001:
          return e % 2 === 0;
        case t.Patterns.PATTERN010:
          return i % 3 === 0;
        case t.Patterns.PATTERN011:
          return (e + i) % 3 === 0;
        case t.Patterns.PATTERN100:
          return (Math.floor(e / 2) + Math.floor(i / 3)) % 2 === 0;
        case t.Patterns.PATTERN101:
          return e * i % 2 + e * i % 3 === 0;
        case t.Patterns.PATTERN110:
          return (e * i % 2 + e * i % 3) % 2 === 0;
        case t.Patterns.PATTERN111:
          return (e * i % 3 + (e + i) % 2) % 2 === 0;
        default:
          throw new Error("bad maskPattern:" + r);
      }
    }
    t.applyMask = function(e, i) {
      const s = i.size;
      for (let u = 0; u < s; u++)
        for (let a = 0; a < s; a++)
          i.isReserved(a, u) || i.xor(a, u, o(e, a, u));
    }, t.getBestMask = function(e, i) {
      const s = Object.keys(t.Patterns).length;
      let u = 0, a = 1 / 0;
      for (let l = 0; l < s; l++) {
        i(l), t.applyMask(l, e);
        const h = t.getPenaltyN1(e) + t.getPenaltyN2(e) + t.getPenaltyN3(e) + t.getPenaltyN4(e);
        t.applyMask(l, e), h < a && (a = h, u = l);
      }
      return u;
    };
  })(ot)), ot;
}
var J = {}, kt;
function Xt() {
  if (kt) return J;
  kt = 1;
  const t = vt(), n = [
    // L  M  Q  H
    1,
    1,
    1,
    1,
    1,
    1,
    1,
    1,
    1,
    1,
    2,
    2,
    1,
    2,
    2,
    4,
    1,
    2,
    4,
    4,
    2,
    4,
    4,
    4,
    2,
    4,
    6,
    5,
    2,
    4,
    6,
    6,
    2,
    5,
    8,
    8,
    4,
    5,
    8,
    8,
    4,
    5,
    8,
    11,
    4,
    8,
    10,
    11,
    4,
    9,
    12,
    16,
    4,
    9,
    16,
    16,
    6,
    10,
    12,
    18,
    6,
    10,
    17,
    16,
    6,
    11,
    16,
    19,
    6,
    13,
    18,
    21,
    7,
    14,
    21,
    25,
    8,
    16,
    20,
    25,
    8,
    17,
    23,
    25,
    9,
    17,
    23,
    34,
    9,
    18,
    25,
    30,
    10,
    20,
    27,
    32,
    12,
    21,
    29,
    35,
    12,
    23,
    34,
    37,
    12,
    25,
    34,
    40,
    13,
    26,
    35,
    42,
    14,
    28,
    38,
    45,
    15,
    29,
    40,
    48,
    16,
    31,
    43,
    51,
    17,
    33,
    45,
    54,
    18,
    35,
    48,
    57,
    19,
    37,
    51,
    60,
    19,
    38,
    53,
    63,
    20,
    40,
    56,
    66,
    21,
    43,
    59,
    70,
    22,
    45,
    62,
    74,
    24,
    47,
    65,
    77,
    25,
    49,
    68,
    81
  ], o = [
    // L  M  Q  H
    7,
    10,
    13,
    17,
    10,
    16,
    22,
    28,
    15,
    26,
    36,
    44,
    20,
    36,
    52,
    64,
    26,
    48,
    72,
    88,
    36,
    64,
    96,
    112,
    40,
    72,
    108,
    130,
    48,
    88,
    132,
    156,
    60,
    110,
    160,
    192,
    72,
    130,
    192,
    224,
    80,
    150,
    224,
    264,
    96,
    176,
    260,
    308,
    104,
    198,
    288,
    352,
    120,
    216,
    320,
    384,
    132,
    240,
    360,
    432,
    144,
    280,
    408,
    480,
    168,
    308,
    448,
    532,
    180,
    338,
    504,
    588,
    196,
    364,
    546,
    650,
    224,
    416,
    600,
    700,
    224,
    442,
    644,
    750,
    252,
    476,
    690,
    816,
    270,
    504,
    750,
    900,
    300,
    560,
    810,
    960,
    312,
    588,
    870,
    1050,
    336,
    644,
    952,
    1110,
    360,
    700,
    1020,
    1200,
    390,
    728,
    1050,
    1260,
    420,
    784,
    1140,
    1350,
    450,
    812,
    1200,
    1440,
    480,
    868,
    1290,
    1530,
    510,
    924,
    1350,
    1620,
    540,
    980,
    1440,
    1710,
    570,
    1036,
    1530,
    1800,
    570,
    1064,
    1590,
    1890,
    600,
    1120,
    1680,
    1980,
    630,
    1204,
    1770,
    2100,
    660,
    1260,
    1860,
    2220,
    720,
    1316,
    1950,
    2310,
    750,
    1372,
    2040,
    2430
  ];
  return J.getBlocksCount = function(e, i) {
    switch (i) {
      case t.L:
        return n[(e - 1) * 4 + 0];
      case t.M:
        return n[(e - 1) * 4 + 1];
      case t.Q:
        return n[(e - 1) * 4 + 2];
      case t.H:
        return n[(e - 1) * 4 + 3];
      default:
        return;
    }
  }, J.getTotalCodewordsCount = function(e, i) {
    switch (i) {
      case t.L:
        return o[(e - 1) * 4 + 0];
      case t.M:
        return o[(e - 1) * 4 + 1];
      case t.Q:
        return o[(e - 1) * 4 + 2];
      case t.H:
        return o[(e - 1) * 4 + 3];
      default:
        return;
    }
  }, J;
}
var st = {}, K = {}, Lt;
function pe() {
  if (Lt) return K;
  Lt = 1;
  const t = new Uint8Array(512), n = new Uint8Array(256);
  return (function() {
    let r = 1;
    for (let e = 0; e < 255; e++)
      t[e] = r, n[r] = e, r <<= 1, r & 256 && (r ^= 285);
    for (let e = 255; e < 512; e++)
      t[e] = t[e - 255];
  })(), K.log = function(r) {
    if (r < 1) throw new Error("log(" + r + ")");
    return n[r];
  }, K.exp = function(r) {
    return t[r];
  }, K.mul = function(r, e) {
    return r === 0 || e === 0 ? 0 : t[n[r] + n[e]];
  }, K;
}
var Dt;
function ye() {
  return Dt || (Dt = 1, (function(t) {
    const n = pe();
    t.mul = function(r, e) {
      const i = new Uint8Array(r.length + e.length - 1);
      for (let s = 0; s < r.length; s++)
        for (let u = 0; u < e.length; u++)
          i[s + u] ^= n.mul(r[s], e[u]);
      return i;
    }, t.mod = function(r, e) {
      let i = new Uint8Array(r);
      for (; i.length - e.length >= 0; ) {
        const s = i[0];
        for (let a = 0; a < e.length; a++)
          i[a] ^= n.mul(e[a], s);
        let u = 0;
        for (; u < i.length && i[u] === 0; ) u++;
        i = i.slice(u);
      }
      return i;
    }, t.generateECPolynomial = function(r) {
      let e = new Uint8Array([1]);
      for (let i = 0; i < r; i++)
        e = t.mul(e, new Uint8Array([1, n.exp(i)]));
      return e;
    };
  })(st)), st;
}
var at, $t;
function we() {
  if ($t) return at;
  $t = 1;
  const t = ye();
  function n(o) {
    this.genPoly = void 0, this.degree = o, this.degree && this.initialize(this.degree);
  }
  return n.prototype.initialize = function(r) {
    this.degree = r, this.genPoly = t.generateECPolynomial(this.degree);
  }, n.prototype.encode = function(r) {
    if (!this.genPoly)
      throw new Error("Encoder not initialized");
    const e = new Uint8Array(r.length + this.degree);
    e.set(r);
    const i = t.mod(e, this.genPoly), s = this.degree - i.length;
    if (s > 0) {
      const u = new Uint8Array(this.degree);
      return u.set(i, s), u;
    }
    return i;
  }, at = n, at;
}
var ut = {}, lt = {}, ct = {}, Ut;
function te() {
  return Ut || (Ut = 1, ct.isValid = function(n) {
    return !isNaN(n) && n >= 1 && n <= 40;
  }), ct;
}
var D = {}, Ft;
function ee() {
  if (Ft) return D;
  Ft = 1;
  const t = "[0-9]+", n = "[A-Z $%*+\\-./:]+";
  let o = "(?:[u3000-u303F]|[u3040-u309F]|[u30A0-u30FF]|[uFF00-uFFEF]|[u4E00-u9FAF]|[u2605-u2606]|[u2190-u2195]|u203B|[u2010u2015u2018u2019u2025u2026u201Cu201Du2225u2260]|[u0391-u0451]|[u00A7u00A8u00B1u00B4u00D7u00F7])+";
  o = o.replace(/u/g, "\\u");
  const r = "(?:(?![A-Z0-9 $%*+\\-./:]|" + o + `)(?:.|[\r
]))+`;
  D.KANJI = new RegExp(o, "g"), D.BYTE_KANJI = new RegExp("[^A-Z0-9 $%*+\\-./:]+", "g"), D.BYTE = new RegExp(r, "g"), D.NUMERIC = new RegExp(t, "g"), D.ALPHANUMERIC = new RegExp(n, "g");
  const e = new RegExp("^" + o + "$"), i = new RegExp("^" + t + "$"), s = new RegExp("^[A-Z0-9 $%*+\\-./:]+$");
  return D.testKanji = function(a) {
    return e.test(a);
  }, D.testNumeric = function(a) {
    return i.test(a);
  }, D.testAlphanumeric = function(a) {
    return s.test(a);
  }, D;
}
var _t;
function x() {
  return _t || (_t = 1, (function(t) {
    const n = te(), o = ee();
    t.NUMERIC = {
      id: "Numeric",
      bit: 1,
      ccBits: [10, 12, 14]
    }, t.ALPHANUMERIC = {
      id: "Alphanumeric",
      bit: 2,
      ccBits: [9, 11, 13]
    }, t.BYTE = {
      id: "Byte",
      bit: 4,
      ccBits: [8, 16, 16]
    }, t.KANJI = {
      id: "Kanji",
      bit: 8,
      ccBits: [8, 10, 12]
    }, t.MIXED = {
      bit: -1
    }, t.getCharCountIndicator = function(i, s) {
      if (!i.ccBits) throw new Error("Invalid mode: " + i);
      if (!n.isValid(s))
        throw new Error("Invalid version: " + s);
      return s >= 1 && s < 10 ? i.ccBits[0] : s < 27 ? i.ccBits[1] : i.ccBits[2];
    }, t.getBestModeForData = function(i) {
      return o.testNumeric(i) ? t.NUMERIC : o.testAlphanumeric(i) ? t.ALPHANUMERIC : o.testKanji(i) ? t.KANJI : t.BYTE;
    }, t.toString = function(i) {
      if (i && i.id) return i.id;
      throw new Error("Invalid mode");
    }, t.isValid = function(i) {
      return i && i.bit && i.ccBits;
    };
    function r(e) {
      if (typeof e != "string")
        throw new Error("Param is not a string");
      switch (e.toLowerCase()) {
        case "numeric":
          return t.NUMERIC;
        case "alphanumeric":
          return t.ALPHANUMERIC;
        case "kanji":
          return t.KANJI;
        case "byte":
          return t.BYTE;
        default:
          throw new Error("Unknown mode: " + e);
      }
    }
    t.from = function(i, s) {
      if (t.isValid(i))
        return i;
      try {
        return r(i);
      } catch {
        return s;
      }
    };
  })(lt)), lt;
}
var Ht;
function be() {
  return Ht || (Ht = 1, (function(t) {
    const n = H(), o = Xt(), r = vt(), e = x(), i = te(), s = 7973, u = n.getBCHDigit(s);
    function a(g, c, B) {
      for (let T = 1; T <= 40; T++)
        if (c <= t.getCapacity(T, B, g))
          return T;
    }
    function l(g, c) {
      return e.getCharCountIndicator(g, c) + 4;
    }
    function h(g, c) {
      let B = 0;
      return g.forEach(function(T) {
        const q = l(T.mode, c);
        B += q + T.getBitsLength();
      }), B;
    }
    function E(g, c) {
      for (let B = 1; B <= 40; B++)
        if (h(g, B) <= t.getCapacity(B, c, e.MIXED))
          return B;
    }
    t.from = function(c, B) {
      return i.isValid(c) ? parseInt(c, 10) : B;
    }, t.getCapacity = function(c, B, T) {
      if (!i.isValid(c))
        throw new Error("Invalid QR Code version");
      typeof T > "u" && (T = e.BYTE);
      const q = n.getSymbolTotalCodewords(c), A = o.getTotalCodewordsCount(c, B), I = (q - A) * 8;
      if (T === e.MIXED) return I;
      const R = I - l(T, c);
      switch (T) {
        case e.NUMERIC:
          return Math.floor(R / 10 * 3);
        case e.ALPHANUMERIC:
          return Math.floor(R / 11 * 2);
        case e.KANJI:
          return Math.floor(R / 13);
        case e.BYTE:
        default:
          return Math.floor(R / 8);
      }
    }, t.getBestVersionForData = function(c, B) {
      let T;
      const q = r.from(B, r.M);
      if (Array.isArray(c)) {
        if (c.length > 1)
          return E(c, q);
        if (c.length === 0)
          return 1;
        T = c[0];
      } else
        T = c;
      return a(T.mode, T.getLength(), q);
    }, t.getEncodedBits = function(c) {
      if (!i.isValid(c) || c < 7)
        throw new Error("Invalid QR Code version");
      let B = c << 12;
      for (; n.getBCHDigit(B) - u >= 0; )
        B ^= s << n.getBCHDigit(B) - u;
      return c << 12 | B;
    };
  })(ut)), ut;
}
var dt = {}, xt;
function Ee() {
  if (xt) return dt;
  xt = 1;
  const t = H(), n = 1335, o = 21522, r = t.getBCHDigit(n);
  return dt.getEncodedBits = function(i, s) {
    const u = i.bit << 3 | s;
    let a = u << 10;
    for (; t.getBCHDigit(a) - r >= 0; )
      a ^= n << t.getBCHDigit(a) - r;
    return (u << 10 | a) ^ o;
  }, dt;
}
var ht = {}, ft, zt;
function ve() {
  if (zt) return ft;
  zt = 1;
  const t = x();
  function n(o) {
    this.mode = t.NUMERIC, this.data = o.toString();
  }
  return n.getBitsLength = function(r) {
    return 10 * Math.floor(r / 3) + (r % 3 ? r % 3 * 3 + 1 : 0);
  }, n.prototype.getLength = function() {
    return this.data.length;
  }, n.prototype.getBitsLength = function() {
    return n.getBitsLength(this.data.length);
  }, n.prototype.write = function(r) {
    let e, i, s;
    for (e = 0; e + 3 <= this.data.length; e += 3)
      i = this.data.substr(e, 3), s = parseInt(i, 10), r.put(s, 10);
    const u = this.data.length - e;
    u > 0 && (i = this.data.substr(e), s = parseInt(i, 10), r.put(s, u * 3 + 1));
  }, ft = n, ft;
}
var gt, Ot;
function Ce() {
  if (Ot) return gt;
  Ot = 1;
  const t = x(), n = [
    "0",
    "1",
    "2",
    "3",
    "4",
    "5",
    "6",
    "7",
    "8",
    "9",
    "A",
    "B",
    "C",
    "D",
    "E",
    "F",
    "G",
    "H",
    "I",
    "J",
    "K",
    "L",
    "M",
    "N",
    "O",
    "P",
    "Q",
    "R",
    "S",
    "T",
    "U",
    "V",
    "W",
    "X",
    "Y",
    "Z",
    " ",
    "$",
    "%",
    "*",
    "+",
    "-",
    ".",
    "/",
    ":"
  ];
  function o(r) {
    this.mode = t.ALPHANUMERIC, this.data = r;
  }
  return o.getBitsLength = function(e) {
    return 11 * Math.floor(e / 2) + 6 * (e % 2);
  }, o.prototype.getLength = function() {
    return this.data.length;
  }, o.prototype.getBitsLength = function() {
    return o.getBitsLength(this.data.length);
  }, o.prototype.write = function(e) {
    let i;
    for (i = 0; i + 2 <= this.data.length; i += 2) {
      let s = n.indexOf(this.data[i]) * 45;
      s += n.indexOf(this.data[i + 1]), e.put(s, 11);
    }
    this.data.length % 2 && e.put(n.indexOf(this.data[i]), 6);
  }, gt = o, gt;
}
var mt, Vt;
function Ae() {
  if (Vt) return mt;
  Vt = 1;
  const t = x();
  function n(o) {
    this.mode = t.BYTE, typeof o == "string" ? this.data = new TextEncoder().encode(o) : this.data = new Uint8Array(o);
  }
  return n.getBitsLength = function(r) {
    return r * 8;
  }, n.prototype.getLength = function() {
    return this.data.length;
  }, n.prototype.getBitsLength = function() {
    return n.getBitsLength(this.data.length);
  }, n.prototype.write = function(o) {
    for (let r = 0, e = this.data.length; r < e; r++)
      o.put(this.data[r], 8);
  }, mt = n, mt;
}
var pt, Kt;
function Re() {
  if (Kt) return pt;
  Kt = 1;
  const t = x(), n = H();
  function o(r) {
    this.mode = t.KANJI, this.data = r;
  }
  return o.getBitsLength = function(e) {
    return e * 13;
  }, o.prototype.getLength = function() {
    return this.data.length;
  }, o.prototype.getBitsLength = function() {
    return o.getBitsLength(this.data.length);
  }, o.prototype.write = function(r) {
    let e;
    for (e = 0; e < this.data.length; e++) {
      let i = n.toSJIS(this.data[e]);
      if (i >= 33088 && i <= 40956)
        i -= 33088;
      else if (i >= 57408 && i <= 60351)
        i -= 49472;
      else
        throw new Error(
          "Invalid SJIS character: " + this.data[e] + `
Make sure your charset is UTF-8`
        );
      i = (i >>> 8 & 255) * 192 + (i & 255), r.put(i, 13);
    }
  }, pt = o, pt;
}
var yt = { exports: {} }, jt;
function Se() {
  return jt || (jt = 1, (function(t) {
    var n = {
      single_source_shortest_paths: function(o, r, e) {
        var i = {}, s = {};
        s[r] = 0;
        var u = n.PriorityQueue.make();
        u.push(r, 0);
        for (var a, l, h, E, g, c, B, T, q; !u.empty(); ) {
          a = u.pop(), l = a.value, E = a.cost, g = o[l] || {};
          for (h in g)
            g.hasOwnProperty(h) && (c = g[h], B = E + c, T = s[h], q = typeof s[h] > "u", (q || T > B) && (s[h] = B, u.push(h, B), i[h] = l));
        }
        if (typeof e < "u" && typeof s[e] > "u") {
          var A = ["Could not find a path from ", r, " to ", e, "."].join("");
          throw new Error(A);
        }
        return i;
      },
      extract_shortest_path_from_predecessor_list: function(o, r) {
        for (var e = [], i = r; i; )
          e.push(i), o[i], i = o[i];
        return e.reverse(), e;
      },
      find_path: function(o, r, e) {
        var i = n.single_source_shortest_paths(o, r, e);
        return n.extract_shortest_path_from_predecessor_list(
          i,
          e
        );
      },
      /**
       * A very naive priority queue implementation.
       */
      PriorityQueue: {
        make: function(o) {
          var r = n.PriorityQueue, e = {}, i;
          o = o || {};
          for (i in r)
            r.hasOwnProperty(i) && (e[i] = r[i]);
          return e.queue = [], e.sorter = o.sorter || r.default_sorter, e;
        },
        default_sorter: function(o, r) {
          return o.cost - r.cost;
        },
        /**
         * Add a new item to the queue and ensure the highest priority element
         * is at the front of the queue.
         */
        push: function(o, r) {
          var e = { value: o, cost: r };
          this.queue.push(e), this.queue.sort(this.sorter);
        },
        /**
         * Return the highest priority element in the queue.
         */
        pop: function() {
          return this.queue.shift();
        },
        empty: function() {
          return this.queue.length === 0;
        }
      }
    };
    t.exports = n;
  })(yt)), yt.exports;
}
var Jt;
function Be() {
  return Jt || (Jt = 1, (function(t) {
    const n = x(), o = ve(), r = Ce(), e = Ae(), i = Re(), s = ee(), u = H(), a = Se();
    function l(A) {
      return unescape(encodeURIComponent(A)).length;
    }
    function h(A, I, R) {
      const v = [];
      let k;
      for (; (k = A.exec(R)) !== null; )
        v.push({
          data: k[0],
          index: k.index,
          mode: I,
          length: k[0].length
        });
      return v;
    }
    function E(A) {
      const I = h(s.NUMERIC, n.NUMERIC, A), R = h(s.ALPHANUMERIC, n.ALPHANUMERIC, A);
      let v, k;
      return u.isKanjiModeEnabled() ? (v = h(s.BYTE, n.BYTE, A), k = h(s.KANJI, n.KANJI, A)) : (v = h(s.BYTE_KANJI, n.BYTE, A), k = []), I.concat(R, v, k).sort(function(w, y) {
        return w.index - y.index;
      }).map(function(w) {
        return {
          data: w.data,
          mode: w.mode,
          length: w.length
        };
      });
    }
    function g(A, I) {
      switch (I) {
        case n.NUMERIC:
          return o.getBitsLength(A);
        case n.ALPHANUMERIC:
          return r.getBitsLength(A);
        case n.KANJI:
          return i.getBitsLength(A);
        case n.BYTE:
          return e.getBitsLength(A);
      }
    }
    function c(A) {
      return A.reduce(function(I, R) {
        const v = I.length - 1 >= 0 ? I[I.length - 1] : null;
        return v && v.mode === R.mode ? (I[I.length - 1].data += R.data, I) : (I.push(R), I);
      }, []);
    }
    function B(A) {
      const I = [];
      for (let R = 0; R < A.length; R++) {
        const v = A[R];
        switch (v.mode) {
          case n.NUMERIC:
            I.push([
              v,
              { data: v.data, mode: n.ALPHANUMERIC, length: v.length },
              { data: v.data, mode: n.BYTE, length: v.length }
            ]);
            break;
          case n.ALPHANUMERIC:
            I.push([
              v,
              { data: v.data, mode: n.BYTE, length: v.length }
            ]);
            break;
          case n.KANJI:
            I.push([
              v,
              { data: v.data, mode: n.BYTE, length: l(v.data) }
            ]);
            break;
          case n.BYTE:
            I.push([
              { data: v.data, mode: n.BYTE, length: l(v.data) }
            ]);
        }
      }
      return I;
    }
    function T(A, I) {
      const R = {}, v = { start: {} };
      let k = ["start"];
      for (let f = 0; f < A.length; f++) {
        const w = A[f], y = [];
        for (let d = 0; d < w.length; d++) {
          const C = w[d], m = "" + f + d;
          y.push(m), R[m] = { node: C, lastCount: 0 }, v[m] = {};
          for (let b = 0; b < k.length; b++) {
            const p = k[b];
            R[p] && R[p].node.mode === C.mode ? (v[p][m] = g(R[p].lastCount + C.length, C.mode) - g(R[p].lastCount, C.mode), R[p].lastCount += C.length) : (R[p] && (R[p].lastCount = C.length), v[p][m] = g(C.length, C.mode) + 4 + n.getCharCountIndicator(C.mode, I));
          }
        }
        k = y;
      }
      for (let f = 0; f < k.length; f++)
        v[k[f]].end = 0;
      return { map: v, table: R };
    }
    function q(A, I) {
      let R;
      const v = n.getBestModeForData(A);
      if (R = n.from(I, v), R !== n.BYTE && R.bit < v.bit)
        throw new Error('"' + A + '" cannot be encoded with mode ' + n.toString(R) + `.
 Suggested mode is: ` + n.toString(v));
      switch (R === n.KANJI && !u.isKanjiModeEnabled() && (R = n.BYTE), R) {
        case n.NUMERIC:
          return new o(A);
        case n.ALPHANUMERIC:
          return new r(A);
        case n.KANJI:
          return new i(A);
        case n.BYTE:
          return new e(A);
      }
    }
    t.fromArray = function(I) {
      return I.reduce(function(R, v) {
        return typeof v == "string" ? R.push(q(v, null)) : v.data && R.push(q(v.data, v.mode)), R;
      }, []);
    }, t.fromString = function(I, R) {
      const v = E(I, u.isKanjiModeEnabled()), k = B(v), f = T(k, R), w = a.find_path(f.map, "start", "end"), y = [];
      for (let d = 1; d < w.length - 1; d++)
        y.push(f.table[w[d]].node);
      return t.fromArray(c(y));
    }, t.rawSplit = function(I) {
      return t.fromArray(
        E(I, u.isKanjiModeEnabled())
      );
    };
  })(ht)), ht;
}
var Qt;
function Te() {
  if (Qt) return X;
  Qt = 1;
  const t = H(), n = vt(), o = de(), r = he(), e = fe(), i = ge(), s = me(), u = Xt(), a = we(), l = be(), h = Ee(), E = x(), g = Be();
  function c(f, w) {
    const y = f.size, d = i.getPositions(w);
    for (let C = 0; C < d.length; C++) {
      const m = d[C][0], b = d[C][1];
      for (let p = -1; p <= 7; p++)
        if (!(m + p <= -1 || y <= m + p))
          for (let S = -1; S <= 7; S++)
            b + S <= -1 || y <= b + S || (p >= 0 && p <= 6 && (S === 0 || S === 6) || S >= 0 && S <= 6 && (p === 0 || p === 6) || p >= 2 && p <= 4 && S >= 2 && S <= 4 ? f.set(m + p, b + S, !0, !0) : f.set(m + p, b + S, !1, !0));
    }
  }
  function B(f) {
    const w = f.size;
    for (let y = 8; y < w - 8; y++) {
      const d = y % 2 === 0;
      f.set(y, 6, d, !0), f.set(6, y, d, !0);
    }
  }
  function T(f, w) {
    const y = e.getPositions(w);
    for (let d = 0; d < y.length; d++) {
      const C = y[d][0], m = y[d][1];
      for (let b = -2; b <= 2; b++)
        for (let p = -2; p <= 2; p++)
          b === -2 || b === 2 || p === -2 || p === 2 || b === 0 && p === 0 ? f.set(C + b, m + p, !0, !0) : f.set(C + b, m + p, !1, !0);
    }
  }
  function q(f, w) {
    const y = f.size, d = l.getEncodedBits(w);
    let C, m, b;
    for (let p = 0; p < 18; p++)
      C = Math.floor(p / 3), m = p % 3 + y - 8 - 3, b = (d >> p & 1) === 1, f.set(C, m, b, !0), f.set(m, C, b, !0);
  }
  function A(f, w, y) {
    const d = f.size, C = h.getEncodedBits(w, y);
    let m, b;
    for (m = 0; m < 15; m++)
      b = (C >> m & 1) === 1, m < 6 ? f.set(m, 8, b, !0) : m < 8 ? f.set(m + 1, 8, b, !0) : f.set(d - 15 + m, 8, b, !0), m < 8 ? f.set(8, d - m - 1, b, !0) : m < 9 ? f.set(8, 15 - m - 1 + 1, b, !0) : f.set(8, 15 - m - 1, b, !0);
    f.set(d - 8, 8, 1, !0);
  }
  function I(f, w) {
    const y = f.size;
    let d = -1, C = y - 1, m = 7, b = 0;
    for (let p = y - 1; p > 0; p -= 2)
      for (p === 6 && p--; ; ) {
        for (let S = 0; S < 2; S++)
          if (!f.isReserved(C, p - S)) {
            let F = !1;
            b < w.length && (F = (w[b] >>> m & 1) === 1), f.set(C, p - S, F), m--, m === -1 && (b++, m = 7);
          }
        if (C += d, C < 0 || y <= C) {
          C -= d, d = -d;
          break;
        }
      }
  }
  function R(f, w, y) {
    const d = new o();
    y.forEach(function(S) {
      d.put(S.mode.bit, 4), d.put(S.getLength(), E.getCharCountIndicator(S.mode, f)), S.write(d);
    });
    const C = t.getSymbolTotalCodewords(f), m = u.getTotalCodewordsCount(f, w), b = (C - m) * 8;
    for (d.getLengthInBits() + 4 <= b && d.put(0, 4); d.getLengthInBits() % 8 !== 0; )
      d.putBit(0);
    const p = (b - d.getLengthInBits()) / 8;
    for (let S = 0; S < p; S++)
      d.put(S % 2 ? 17 : 236, 8);
    return v(d, f, w);
  }
  function v(f, w, y) {
    const d = t.getSymbolTotalCodewords(w), C = u.getTotalCodewordsCount(w, y), m = d - C, b = u.getBlocksCount(w, y), p = d % b, S = b - p, F = Math.floor(d / b), V = Math.floor(m / b), re = V + 1, Ct = F - V, ie = new a(Ct);
    let Q = 0;
    const j = new Array(b), At = new Array(b);
    let Y = 0;
    const oe = new Uint8Array(f.buffer);
    for (let z = 0; z < b; z++) {
      const W = z < S ? V : re;
      j[z] = oe.slice(Q, Q + W), At[z] = ie.encode(j[z]), Q += W, Y = Math.max(Y, W);
    }
    const G = new Uint8Array(d);
    let Rt = 0, $, U;
    for ($ = 0; $ < Y; $++)
      for (U = 0; U < b; U++)
        $ < j[U].length && (G[Rt++] = j[U][$]);
    for ($ = 0; $ < Ct; $++)
      for (U = 0; U < b; U++)
        G[Rt++] = At[U][$];
    return G;
  }
  function k(f, w, y, d) {
    let C;
    if (Array.isArray(f))
      C = g.fromArray(f);
    else if (typeof f == "string") {
      let F = w;
      if (!F) {
        const V = g.rawSplit(f);
        F = l.getBestVersionForData(V, y);
      }
      C = g.fromString(f, F || 40);
    } else
      throw new Error("Invalid data");
    const m = l.getBestVersionForData(C, y);
    if (!m)
      throw new Error("The amount of data is too big to be stored in a QR Code");
    if (!w)
      w = m;
    else if (w < m)
      throw new Error(
        `
The chosen QR Code version cannot contain this amount of data.
Minimum version required to store current data is: ` + m + `.
`
      );
    const b = R(w, y, C), p = t.getSymbolSize(w), S = new r(p);
    return c(S, w), B(S), T(S, w), A(S, y, 0), w >= 7 && q(S, w), I(S, b), isNaN(d) && (d = s.getBestMask(
      S,
      A.bind(null, S, y)
    )), s.applyMask(d, S), A(S, y, d), {
      modules: S,
      version: w,
      errorCorrectionLevel: y,
      maskPattern: d,
      segments: C
    };
  }
  return X.create = function(w, y) {
    if (typeof w > "u" || w === "")
      throw new Error("No input text");
    let d = n.M, C, m;
    return typeof y < "u" && (d = n.from(y.errorCorrectionLevel, n.M), C = l.from(y.version), m = s.from(y.maskPattern), y.toSJISFunc && t.setToSJISFunction(y.toSJISFunc)), k(w, C, d, m);
  }, X;
}
var wt = {}, bt = {}, Yt;
function ne() {
  return Yt || (Yt = 1, (function(t) {
    function n(o) {
      if (typeof o == "number" && (o = o.toString()), typeof o != "string")
        throw new Error("Color should be defined as hex string");
      let r = o.slice().replace("#", "").split("");
      if (r.length < 3 || r.length === 5 || r.length > 8)
        throw new Error("Invalid hex color: " + o);
      (r.length === 3 || r.length === 4) && (r = Array.prototype.concat.apply([], r.map(function(i) {
        return [i, i];
      }))), r.length === 6 && r.push("F", "F");
      const e = parseInt(r.join(""), 16);
      return {
        r: e >> 24 & 255,
        g: e >> 16 & 255,
        b: e >> 8 & 255,
        a: e & 255,
        hex: "#" + r.slice(0, 6).join("")
      };
    }
    t.getOptions = function(r) {
      r || (r = {}), r.color || (r.color = {});
      const e = typeof r.margin > "u" || r.margin === null || r.margin < 0 ? 4 : r.margin, i = r.width && r.width >= 21 ? r.width : void 0, s = r.scale || 4;
      return {
        width: i,
        scale: i ? 4 : s,
        margin: e,
        color: {
          dark: n(r.color.dark || "#000000ff"),
          light: n(r.color.light || "#ffffffff")
        },
        type: r.type,
        rendererOpts: r.rendererOpts || {}
      };
    }, t.getScale = function(r, e) {
      return e.width && e.width >= r + e.margin * 2 ? e.width / (r + e.margin * 2) : e.scale;
    }, t.getImageWidth = function(r, e) {
      const i = t.getScale(r, e);
      return Math.floor((r + e.margin * 2) * i);
    }, t.qrToImageData = function(r, e, i) {
      const s = e.modules.size, u = e.modules.data, a = t.getScale(s, i), l = Math.floor((s + i.margin * 2) * a), h = i.margin * a, E = [i.color.light, i.color.dark];
      for (let g = 0; g < l; g++)
        for (let c = 0; c < l; c++) {
          let B = (g * l + c) * 4, T = i.color.light;
          if (g >= h && c >= h && g < l - h && c < l - h) {
            const q = Math.floor((g - h) / a), A = Math.floor((c - h) / a);
            T = E[u[q * s + A] ? 1 : 0];
          }
          r[B++] = T.r, r[B++] = T.g, r[B++] = T.b, r[B] = T.a;
        }
    };
  })(bt)), bt;
}
var Gt;
function Ie() {
  return Gt || (Gt = 1, (function(t) {
    const n = ne();
    function o(e, i, s) {
      e.clearRect(0, 0, i.width, i.height), i.style || (i.style = {}), i.height = s, i.width = s, i.style.height = s + "px", i.style.width = s + "px";
    }
    function r() {
      try {
        return document.createElement("canvas");
      } catch {
        throw new Error("You need to specify a canvas element");
      }
    }
    t.render = function(i, s, u) {
      let a = u, l = s;
      typeof a > "u" && (!s || !s.getContext) && (a = s, s = void 0), s || (l = r()), a = n.getOptions(a);
      const h = n.getImageWidth(i.modules.size, a), E = l.getContext("2d"), g = E.createImageData(h, h);
      return n.qrToImageData(g.data, i, a), o(E, l, h), E.putImageData(g, 0, 0), l;
    }, t.renderToDataURL = function(i, s, u) {
      let a = u;
      typeof a > "u" && (!s || !s.getContext) && (a = s, s = void 0), a || (a = {});
      const l = t.render(i, s, a), h = a.type || "image/png", E = a.rendererOpts || {};
      return l.toDataURL(h, E.quality);
    };
  })(wt)), wt;
}
var Et = {}, Wt;
function Pe() {
  if (Wt) return Et;
  Wt = 1;
  const t = ne();
  function n(e, i) {
    const s = e.a / 255, u = i + '="' + e.hex + '"';
    return s < 1 ? u + " " + i + '-opacity="' + s.toFixed(2).slice(1) + '"' : u;
  }
  function o(e, i, s) {
    let u = e + i;
    return typeof s < "u" && (u += " " + s), u;
  }
  function r(e, i, s) {
    let u = "", a = 0, l = !1, h = 0;
    for (let E = 0; E < e.length; E++) {
      const g = Math.floor(E % i), c = Math.floor(E / i);
      !g && !l && (l = !0), e[E] ? (h++, E > 0 && g > 0 && e[E - 1] || (u += l ? o("M", g + s, 0.5 + c + s) : o("m", a, 0), a = 0, l = !1), g + 1 < i && e[E + 1] || (u += o("h", h), h = 0)) : a++;
    }
    return u;
  }
  return Et.render = function(i, s, u) {
    const a = t.getOptions(s), l = i.modules.size, h = i.modules.data, E = l + a.margin * 2, g = a.color.light.a ? "<path " + n(a.color.light, "fill") + ' d="M0 0h' + E + "v" + E + 'H0z"/>' : "", c = "<path " + n(a.color.dark, "stroke") + ' d="' + r(h, l, a.margin) + '"/>', B = 'viewBox="0 0 ' + E + " " + E + '"', q = '<svg xmlns="http://www.w3.org/2000/svg" ' + (a.width ? 'width="' + a.width + '" height="' + a.width + '" ' : "") + B + ' shape-rendering="crispEdges">' + g + c + `</svg>
`;
    return typeof u == "function" && u(null, q), q;
  }, Et;
}
var Zt;
function Me() {
  if (Zt) return O;
  Zt = 1;
  const t = ce(), n = Te(), o = Ie(), r = Pe();
  function e(i, s, u, a, l) {
    const h = [].slice.call(arguments, 1), E = h.length, g = typeof h[E - 1] == "function";
    if (!g && !t())
      throw new Error("Callback required as last argument");
    if (g) {
      if (E < 2)
        throw new Error("Too few arguments provided");
      E === 2 ? (l = u, u = s, s = a = void 0) : E === 3 && (s.getContext && typeof l > "u" ? (l = a, a = void 0) : (l = a, a = u, u = s, s = void 0));
    } else {
      if (E < 1)
        throw new Error("Too few arguments provided");
      return E === 1 ? (u = s, s = a = void 0) : E === 2 && !s.getContext && (a = u, u = s, s = void 0), new Promise(function(c, B) {
        try {
          const T = n.create(u, a);
          c(i(T, s, a));
        } catch (T) {
          B(T);
        }
      });
    }
    try {
      const c = n.create(u, a);
      l(null, i(c, s, a));
    } catch (c) {
      l(c);
    }
  }
  return O.create = n.create, O.toCanvas = e.bind(null, o.render), O.toDataURL = e.bind(null, o.renderToDataURL), O.toString = e.bind(null, function(i, s, u) {
    return r.render(i, u);
  }), O;
}
var Ne = Me(), qe = Object.defineProperty, ke = Object.getOwnPropertyDescriptor, M = (t, n, o, r) => {
  for (var e = r > 1 ? void 0 : r ? ke(n, o) : n, i = t.length - 1, s; i >= 0; i--)
    (s = t[i]) && (e = (r ? s(n, o, e) : s(e)) || e);
  return r && e && qe(n, o, e), e;
};
let P = class extends le(se) {
  constructor() {
    super(), this.payments = [], this.filteredPayments = [], this.searchTerm = "", this.connected = !1, this.offlineMode = !1, this.minSat = null, this.maxSat = null, this.recommendedFees = null, this.loadingStatus = !0, this.errorStatus = "", this.health = null, this.quoteAmount = 1e3, this.quoting = !1, this.quoteError = "", this.quoteResult = null, this.testAmount = 1e3, this.testDescription = "Test invoice", this.creatingInvoice = !1, this.createdInvoice = null, this.createdPaymentHash = null, this.invoiceQrDataUrl = null, this.invoiceError = "", this.autoRefresh = !1, this.refreshTimer = null, this.refreshIntervalMs = 1e4, this.refreshing = !1, this.copyOk = !1, this.rowActionBusy = {}, this.rowActionError = {}, this.onRefreshClick = async () => {
      this.refreshing = !0;
      try {
        await this.loadAll();
      } finally {
        this.refreshing = !1;
      }
    }, this.toggleAutoRefresh = (t) => {
      const n = t.target.checked;
      this.autoRefresh = n, n ? this.startAutoRefresh() : this.stopAutoRefresh();
    }, this.loadAll();
  }
  connectedCallback() {
    super.connectedCallback(), this.autoRefresh && this.startAutoRefresh();
  }
  disconnectedCallback() {
    super.disconnectedCallback(), this.stopAutoRefresh();
  }
  async loadAll() {
    await Promise.all([
      this.loadStatus(),
      this.loadLimits(),
      this.loadRecommendedFees(),
      this.loadHealth(),
      this.loadPayments()
    ]);
  }
  async loadStatus() {
    this.loadingStatus = !0, this.errorStatus = "";
    try {
      const t = await fetch(
        "/umbraco/management/api/lightningpayments/GetStatus"
      );
      if (!t.ok) throw new Error(`HTTP ${t.status}`);
      const n = await t.json();
      this.connected = !!n.connected, this.offlineMode = !!n.offlineMode;
    } catch (t) {
      this.errorStatus = t?.message ?? "Failed to load status";
    } finally {
      this.loadingStatus = !1;
    }
  }
  async loadLimits() {
    try {
      const t = await fetch(
        "/umbraco/management/api/lightningpayments/GetLightningReceiveLimits"
      );
      if (!t.ok) return;
      const n = await t.json();
      this.minSat = typeof n.minSat == "number" ? n.minSat : null, this.maxSat = typeof n.maxSat == "number" ? n.maxSat : null;
    } catch {
    }
  }
  async loadRecommendedFees() {
    try {
      const t = await fetch(
        "/umbraco/management/api/lightningpayments/GetRecommendedFees"
      );
      if (!t.ok) return;
      this.recommendedFees = await t.json();
    } catch {
    }
  }
  async loadHealth() {
    try {
      const t = await fetch("/health/ready");
      if (!t.ok) {
        this.health = { status: `HTTP ${t.status}` };
        return;
      }
      const n = await t.text();
      this.health = { status: "Healthy", description: n?.substring(0, 120) };
    } catch (t) {
      this.health = { status: "Unknown", description: t?.message };
    }
  }
  async loadPayments() {
    try {
      const t = await fetch(
        "/umbraco/management/api/lightningpayments/GetAllPayments"
      );
      t.ok && (this.payments = await t.json(), this.filteredPayments = this.payments);
    } catch (t) {
      console.error("Failed to load payments:", t);
    }
  }
  handleSearch(t) {
    const n = t.target;
    this.searchTerm = n.value.toLowerCase(), this.filteredPayments = this.payments.filter(
      (o) => o.paymentHash.toLowerCase().includes(this.searchTerm) || o.contentId.toString().includes(this.searchTerm) || o.status.toLowerCase().includes(this.searchTerm)
    );
  }
  async createTestInvoice() {
    this.creatingInvoice = !0, this.invoiceError = "", this.createdInvoice = null, this.createdPaymentHash = null, this.invoiceQrDataUrl = null;
    try {
      const t = await fetch(
        "/umbraco/management/api/lightningpayments/CreateTestInvoice",
        { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ amountSat: this.testAmount, description: this.testDescription }) }
      );
      if (!t.ok) throw new Error(`HTTP ${t.status}`);
      const n = await t.json();
      if (this.createdInvoice = n.invoice, this.createdPaymentHash = n.paymentHash, this.createdInvoice)
        try {
          this.invoiceQrDataUrl = await Ne.toDataURL(this.createdInvoice, { width: 240, margin: 1 });
        } catch (o) {
          console.error("QR generation failed", o);
        }
    } catch (t) {
      this.invoiceError = t?.message ?? "Failed to create invoice";
    } finally {
      this.creatingInvoice = !1;
    }
  }
  async getQuote() {
    this.quoting = !0, this.quoteError = "", this.quoteResult = null;
    try {
      const t = new URL("/umbraco/management/api/lightningpayments/GetPaywallReceiveFeeQuote", location.origin);
      t.searchParams.set("contentId", String(0));
      const n = await fetch(t.toString());
      if (!n.ok) throw new Error(`HTTP ${n.status}`);
      this.quoteResult = await n.json();
    } catch (t) {
      this.quoteError = t?.message ?? "Failed to get quote";
    } finally {
      this.quoting = !1;
    }
  }
  startAutoRefresh() {
    this.stopAutoRefresh(), this.refreshTimer = window.setInterval(() => this.loadAll(), this.refreshIntervalMs);
  }
  stopAutoRefresh() {
    this.refreshTimer && (clearInterval(this.refreshTimer), this.refreshTimer = null);
  }
  async copyInvoice() {
    if (this.createdInvoice)
      try {
        await navigator.clipboard.writeText(this.createdInvoice), this.copyOk = !0, setTimeout(() => this.copyOk = !1, 1500);
      } catch (t) {
        console.warn("Copy failed", t);
      }
  }
  async sendRowAction(t, n) {
    this.rowActionBusy = { ...this.rowActionBusy, [n]: !0 }, this.rowActionError = { ...this.rowActionError, [n]: "" };
    try {
      const o = await fetch(`/umbraco/management/api/lightningpayments/${t}`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ paymentHash: n }) });
      if (!o.ok) throw new Error(`HTTP ${o.status}`);
      await this.loadPayments();
    } catch (o) {
      this.rowActionError = { ...this.rowActionError, [n]: o?.message ?? "Action failed" };
    } finally {
      this.rowActionBusy = { ...this.rowActionBusy, [n]: !1 };
    }
  }
  render() {
    return L`
      <umb-body-layout header-transparent>
        <div slot="header"><h2>Lightning Payments</h2></div>
        <div slot="main">
          ${this.renderStatus()}
          ${this.renderQuote()}
          ${this.renderTestTools()}
          ${this.renderPaymentsTable()}
        </div>
      </umb-body-layout>`;
  }
  renderStatus() {
    return L`
      <uui-box headline="Status" style="margin-bottom: var(--uui-size-layout-1)">
        <div class="toolbar">
          <uui-button look="secondary" @click=${this.onRefreshClick} ?disabled=${this.refreshing}>${this.refreshing ? "Refreshing�" : "Refresh"}</uui-button>
          <label class="auto"><input type="checkbox" .checked=${this.autoRefresh} @change=${this.toggleAutoRefresh} /><span>Auto-refresh</span></label>
        </div>
        ${this.loadingStatus ? L`<div>Loading status�</div>` : this.errorStatus ? L`<uui-alert color="danger">${this.errorStatus}</uui-alert>` : L`
          <div class="status-grid">
            <div><strong>SDK Connected:</strong> ${this.connected ? "Yes" : "No"}</div>
            <div><strong>Offline Mode:</strong> ${this.offlineMode ? "Yes" : "No"}</div>
            <div><strong>Lightning Limits:</strong> ${this.minSat != null && this.maxSat != null ? `${this.minSat} � ${this.maxSat} sats` : "Unknown"}</div>
            <div><strong>Health:</strong> ${this.health?.status ?? "Unknown"}</div>
          </div>
          ${this.recommendedFees ? L`<details class="fees-details"><summary>Recommended on-chain fees</summary><pre>${JSON.stringify(this.recommendedFees, null, 2)}</pre></details>` : ""}
        `}
      </uui-box>`;
  }
  renderQuote() {
    return L`
      <uui-box headline="Receive fee quote" style="margin-bottom: var(--uui-size-layout-1)">
        <div class="quote-row">
          <uui-label for="q-amount">Amount (sats)</uui-label>
          <uui-input id="q-amount" type="number" min="1" step="1" .value=${String(this.quoteAmount)} @input=${(t) => this.quoteAmount = Math.max(1, parseInt(t.target.value || 1, 10))}></uui-input>
          <uui-button look="primary" @click=${this.getQuote} ?disabled=${this.quoting}>${this.quoting ? "Quoting�" : "Get quote"}</uui-button>
        </div>
        ${this.quoteError ? L`<uui-alert color="danger">${this.quoteError}</uui-alert>` : ""}
        ${this.quoteResult ? L`<div class="quote-out">Estimated fees: <strong>${this.quoteResult.feesSat}</strong> sats (${this.quoteResult.method})</div>` : ""}
      </uui-box>`;
  }
  renderTestTools() {
    const t = this.createdInvoice ? `lightning:${this.createdInvoice}` : null;
    return L`
      <uui-box headline="Test invoice" style="margin-bottom: var(--uui-size-layout-1)">
        <div class="test-grid">
          <div>
            <uui-label for="ti-amount">Amount (sats)</uui-label>
            <uui-input
              id="ti-amount"
              type="number"
              min="1"
              step="1"
              .value=${String(this.testAmount)}
              @input=${(n) => this.testAmount = Math.max(1, parseInt(n.target.value || 1, 10))}
            ></uui-input>
          </div>
          <div>
            <uui-label for="ti-desc">Description</uui-label>
            <uui-input
              id="ti-desc"
              type="text"
              .value=${this.testDescription}
              @input=${(n) => this.testDescription = n.target.value}
            ></uui-input>
          </div>
        </div>
        ${this.invoiceError ? L`<uui-alert color="danger">${this.invoiceError}</uui-alert>` : ""}
        <div class="test-actions">
          <uui-button
            look="primary"
            @click=${this.createTestInvoice}
            ?disabled=${this.creatingInvoice}
          >${this.creatingInvoice ? "Creating�" : "Create invoice"}</uui-button>
        </div>
        ${this.createdInvoice ? L`
              <div class="invoice-out">
                ${this.invoiceQrDataUrl ? L`<img class="qr" src="${this.invoiceQrDataUrl}" alt="Invoice QR" />` : ""}
                <div class="invoice-text">
                  <uui-label>Invoice</uui-label>
                  <uui-textarea
                    readonly
                    .value=${this.createdInvoice}
                  ></uui-textarea>
                  <div class="actions">
                    <uui-button look="secondary" @click=${this.copyInvoice}>${this.copyOk ? "Copied" : "Copy"}</uui-button>
                    ${t ? L`<a class="open-wallet" href="${t}">Open in wallet</a>` : ""}
                  </div>
                  <div class="hash">Payment hash: ${this.createdPaymentHash?.slice(0, 12)}�</div>
                </div>
              </div>
            ` : ""}
      </uui-box>
    `;
  }
  renderPaymentsTable() {
    return L`
      <uui-box headline="Payments">
        <div slot="header">
          <input
            type="text"
            placeholder="Search payments..."
            @input=${this.handleSearch}
            style="width:100%; padding:8px; margin-bottom:16px;"
          />
        </div>
        <uui-table>
          <uui-table-head>
            <uui-table-head-cell>Payment Hash</uui-table-head-cell>
            <uui-table-head-cell>Content ID</uui-table-head-cell>
            <uui-table-head-cell>Session ID</uui-table-head-cell>
            <uui-table-head-cell>Status</uui-table-head-cell>
            <uui-table-head-cell style="width:260px">Actions</uui-table-head-cell>
          </uui-table-head>
          <uui-table-body>
            ${this.filteredPayments.map((t) => {
      const n = !!this.rowActionBusy[t.paymentHash], o = this.rowActionError[t.paymentHash];
      return L`
                <uui-table-row>
                  <uui-table-cell>${t.paymentHash}</uui-table-cell>
                  <uui-table-cell>${t.contentId}</uui-table-cell>
                  <uui-table-cell>${t.userSessionId}</uui-table-cell>
                  <uui-table-cell>${t.status}</uui-table-cell>
                  <uui-table-cell>
                    <div class="row-actions">
                      <uui-button look="secondary" compact @click=${() => this.sendRowAction("ConfirmPayment", t.paymentHash)} ?disabled=${n}>Confirm</uui-button>
                      <uui-button look="warning" compact @click=${() => this.sendRowAction("MarkAsFailed", t.paymentHash)} ?disabled=${n}>Fail</uui-button>
                      <uui-button look="danger" compact @click=${() => this.sendRowAction("MarkAsExpired", t.paymentHash)} ?disabled=${n}>Expire</uui-button>
                      <uui-button look="secondary" compact @click=${() => this.sendRowAction("MarkAsRefundPending", t.paymentHash)} ?disabled=${n}>Refund pending</uui-button>
                      <uui-button look="positive" compact @click=${() => this.sendRowAction("MarkAsRefunded", t.paymentHash)} ?disabled=${n}>Refunded</uui-button>
                    </div>
                    ${o ? L`<div class="row-error">${o}</div>` : ""}
                  </uui-table-cell>
                </uui-table-row>`;
    })}
          </uui-table-body>
        </uui-table>
      </uui-box>
    `;
  }
};
P.styles = [
  ae`
      :host { display:block; padding: var(--uui-size-layout-1); }
      uui-box { margin-bottom: var(--uui-size-layout-1); }
      h2 { margin-top:0; }
      .toolbar { display:flex; gap:0.5rem; align-items:center; margin-bottom:0.5rem; }
      .auto { display:flex; gap:0.35rem; align-items:center; color: var(--uui-color-text); }
      .status-grid { display:grid; grid-template-columns: repeat(auto-fit, minmax(200px,1fr)); gap:0.5rem 1rem; }
      .fees-details { margin-top:0.5rem; }
      .quote-row { display:grid; grid-template-columns: 1fr 1fr auto; gap:0.5rem; align-items:end; }
      .quote-out { margin-top:0.5rem; }
      .test-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        gap: 1rem;
        align-items: end;
      }
      .test-actions {
        margin-top: 0.5rem;
      }
      .invoice-out {
        margin-top: 1rem;
        display: grid;
        grid-template-columns: 240px 1fr;
        gap: 1rem;
      }
      .qr {
        width: 240px;
        height: 240px;
        background: white;
        border: 1px solid var(--uui-color-border);
        border-radius: 4px;
        padding: 4px;
      }
      .invoice-text uui-textarea {
        width: 100%;
        height: 120px;
      }
      .actions {
        display: flex;
        gap: 0.5rem;
        align-items: center;
        margin-top: 0.5rem;
      }
      .open-wallet {
        text-decoration: none;
        background: var(--uui-color-highlight);
        color: var(--uui-color-surface);
        padding: 0.4rem 0.6rem;
        border-radius: 4px;
      }
      .hash {
        margin-top: 0.5rem;
        color: var(--uui-color-text-alt);
        font-family: monospace;
      }
      .row-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.25rem;
      }
      .row-error {
        margin-top: 0.25rem;
        color: var(--uui-color-danger);
        font-size: 0.85rem;
      }
    `
];
M([
  N()
], P.prototype, "payments", 2);
M([
  N()
], P.prototype, "filteredPayments", 2);
M([
  N()
], P.prototype, "searchTerm", 2);
M([
  N()
], P.prototype, "connected", 2);
M([
  N()
], P.prototype, "offlineMode", 2);
M([
  N()
], P.prototype, "minSat", 2);
M([
  N()
], P.prototype, "maxSat", 2);
M([
  N()
], P.prototype, "recommendedFees", 2);
M([
  N()
], P.prototype, "loadingStatus", 2);
M([
  N()
], P.prototype, "errorStatus", 2);
M([
  N()
], P.prototype, "health", 2);
M([
  N()
], P.prototype, "quoteAmount", 2);
M([
  N()
], P.prototype, "quoting", 2);
M([
  N()
], P.prototype, "quoteError", 2);
M([
  N()
], P.prototype, "quoteResult", 2);
M([
  N()
], P.prototype, "testAmount", 2);
M([
  N()
], P.prototype, "testDescription", 2);
M([
  N()
], P.prototype, "creatingInvoice", 2);
M([
  N()
], P.prototype, "createdInvoice", 2);
M([
  N()
], P.prototype, "createdPaymentHash", 2);
M([
  N()
], P.prototype, "invoiceQrDataUrl", 2);
M([
  N()
], P.prototype, "invoiceError", 2);
M([
  N()
], P.prototype, "autoRefresh", 2);
M([
  N()
], P.prototype, "refreshing", 2);
M([
  N()
], P.prototype, "copyOk", 2);
M([
  N()
], P.prototype, "rowActionBusy", 2);
M([
  N()
], P.prototype, "rowActionError", 2);
P = M([
  ue("lightning-payments-dashboard")
], P);
const $e = P;
export {
  P as LightningPaymentsDashboardElement,
  $e as default
};
//# sourceMappingURL=dashboard.element-MycfWCqH.js.map
