import React from 'react';
export function Badge({ children, tone = 'accent', style }) {
  const c = { accent: ['var(--platano-500)', 'var(--cafe-900)'], danger: ['var(--chile-500)', '#fff'], dark: ['var(--cafe-900)', 'var(--platano-500)'] }[tone] || [];
  return <span style={{ minWidth: 24, height: 24, padding: '0 7px', borderRadius: 999, display: 'inline-flex', alignItems: 'center', justifyContent: 'center', background: c[0], color: c[1], border: '2px solid var(--cafe-900)', font: '800 13px/1 var(--font-display)', boxSizing: 'border-box', ...style }}>{children}</span>;
}
