import { css } from 'lit';

// Base theme variables for Lightning UI components
// Consumers can override these CSS custom properties on :root or a container.
// Supports prefers-color-scheme and explicit [data-theme="dark"] override.
export const lightningTheme = css`
 :root {
 --lp-color-primary: #f89c1c;
 --lp-color-primary-hover: #e68a0a;
 --lp-color-bg: #ffffff;
 --lp-color-surface: #ffffff;
 --lp-color-text: #222222;
 --lp-color-text-muted: #666666;
 --lp-color-success: #2e7d32;
 --lp-color-danger: #b00020;
 --lp-color-border: #e6e6e6;
 --lp-overlay: rgba(0,0,0,0.6);
 --lp-radius:6px;
 --lp-border:1px solid var(--lp-color-border);
 --lp-shadow: 0 10px 30px rgba(0,0,0,0.15);
 --lp-spacing:0.75rem;
 }

 @media (prefers-color-scheme: dark) {
 :root {
 --lp-color-bg: #0e0e0f;
 --lp-color-surface: #171719;
 --lp-color-text: #e6e6e6;
 --lp-color-text-muted: #a6a6a6;
 --lp-color-border: #2b2b2e;
 --lp-overlay: rgba(0,0,0,0.7);
 --lp-shadow: 0 10px 30px rgba(0,0,0,0.5);
 }
 }

 [data-theme="dark"] {
 --lp-color-bg: #0e0e0f;
 --lp-color-surface: #171719;
 --lp-color-text: #e6e6e6;
 --lp-color-text-muted: #a6a6a6;
 --lp-color-border: #2b2b2e;
 --lp-overlay: rgba(0,0,0,0.7);
 --lp-shadow: 0 10px 30px rgba(0,0,0,0.5);
 }
`;
