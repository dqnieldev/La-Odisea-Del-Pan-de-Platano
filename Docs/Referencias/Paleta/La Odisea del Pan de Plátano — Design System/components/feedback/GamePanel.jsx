import React from 'react';
export function GamePanel({ title, eyebrow, tone = 'light', children, actions, width = 520, style }) {
  const dark = tone === 'dark';
  return (
    <div role="dialog" aria-label={title} style={{ width, maxWidth: '100%', boxSizing: 'border-box', borderRadius: 'var(--radius-xl)', padding: '28px 28px 24px', display: 'flex', flexDirection: 'column', gap: 18,
      background: dark ? 'var(--cafe-900)' : 'var(--crema-50)', color: dark ? 'var(--crema-50)' : 'var(--text-body)', border: `var(--border-w-thick) solid var(--cafe-950)`, boxShadow: 'var(--shadow-pop-lg), var(--shadow-float)', ...style }}>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 6, alignItems: 'center', textAlign: 'center' }}>
        {eyebrow && <span style={{ font: '700 12px/1 var(--font-body)', letterSpacing: '.08em', textTransform: 'uppercase', color: dark ? 'var(--cafe-300)' : 'var(--text-muted)' }}>{eyebrow}</span>}
        <span style={{ font: '800 40px/1.05 var(--font-display)', color: dark ? 'var(--platano-500)' : 'var(--text-strong)', textWrap: 'balance' }}>{title}</span>
      </div>
      {children}
      {actions && <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>{actions}</div>}
    </div>
  );
}
