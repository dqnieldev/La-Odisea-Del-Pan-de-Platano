import React from 'react';
import { Icon } from '../core/Icon.jsx';
export function HudChip({ icon, value, label, accent = false, style }) {
  return (
    <div style={{ display: 'inline-flex', alignItems: 'center', gap: 10, height: 52, padding: '0 18px 0 12px', borderRadius: 'var(--radius-pill)', background: 'var(--cafe-900)', border: 'var(--border-w-thick) solid var(--cafe-950)', boxShadow: 'var(--shadow-pop)', color: accent ? 'var(--platano-500)' : 'var(--crema-50)', ...style }}>
      {icon && <Icon name={icon} size={30} color="var(--platano-500)" />}
      <span style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
        {label && <span style={{ font: '700 10px/1 var(--font-body)', letterSpacing: '.08em', textTransform: 'uppercase', color: 'var(--cafe-300)' }}>{label}</span>}
        <span style={{ font: '800 24px/1 var(--font-display)', fontVariantNumeric: 'tabular-nums' }}>{value}</span>
      </span>
    </div>
  );
}
