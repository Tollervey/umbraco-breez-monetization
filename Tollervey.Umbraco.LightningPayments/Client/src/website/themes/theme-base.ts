import { css } from 'lit';

// Base theme variables for Lightning UI components
// Consumers can override these CSS custom properties on :root or a container.
export const lightningTheme = css`
 :root {
 --lp-color-primary: #f89c1c;
 --lp-color-primary-hover: #e68a0a;
 --lp-color-bg: #ffffff;
 --lp-color-text: #222;
 --lp-color-text-muted: #666;
 --lp-color-success: #2e7d32;
 --lp-color-danger: #b00020;
 --lp-radius:6px;
 --lp-border:1px solid #e6e6e6;
 --lp-shadow:010px30px rgba(0,0,0,0.15);
 --lp-spacing:0.75rem;
 }
`;
