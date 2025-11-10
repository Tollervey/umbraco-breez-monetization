declare module '@umbraco-cms/backoffice/external/lit' {
 // Minimal re-exports used in this project
 export class LitElement extends HTMLElement { renderRoot: ShadowRoot; }
 export const html: any;
 export const css: any;
 export const customElement: any;
 export const property: any;
 export const state: any;
 const _default: any;
 export default _default;
}

declare module '@umbraco-cms/backoffice/element-api' {
 // UmbElementMixin is a mixin function that returns a class extending the base
 export function UmbElementMixin<TBase extends new (...args: any[]) => HTMLElement>(base: TBase): TBase;
}

declare module '@umbraco-cms/backoffice/extension-registry' {
 export type UmbExtensionManifest = any;
}

declare module 'lit' {
 export class LitElement extends HTMLElement { renderRoot: ShadowRoot; }
 export const html: any;
 export const css: any;
 export const nothing: any;
}

declare module 'lit/decorators.js' {
 export const customElement: any;
 export const property: any;
 export const state: any;
}

declare module 'vite' {
 export function defineConfig(config: any): any;
}
