import React from 'react';
import { Icon } from './Icon.jsx';
export function IconButton({ icon, label, size = 48, variant = 'light', disabled = false, onClick, style }) {
  const [down, setDown] = React.useState(false);
  const p = down && !disabled;
  const V = { light: ['var(--surface-card)', 'var(--beige-100)', 'var(--text-strong)'], accent: ['var(--accent)', 'var(--accent-pressed)', 'var(--cafe-900)'], dark: ['var(--cafe-900)', 'var(--cafe-950)', 'var(--platano-500)'] }[variant] || [];
  return (
    <button type="button" aria-label={label} title={label} disabled={disabled} onClick={onClick}
      onPointerDown={() => setDown(true)} onPointerUp={() => setDown(false)} onPointerLeave={() => setDown(false)}
      style={{ width: size, height: size, borderRadius: '50%', display: 'inline-grid', placeItems: 'center', flex: 'none',
        border: `var(--border-w) solid ${disabled ? 'var(--beige-300)' : 'var(--border-strong)'}`, background: disabled ? 'var(--disabled-bg)' : p ? V[1] : V[0],
        color: disabled ? 'var(--disabled-fg)' : V[2], cursor: disabled ? 'not-allowed' : 'pointer', padding: 0,
        boxShadow: disabled ? 'none' : p ? 'var(--shadow-pop-sm)' : 'var(--shadow-pop)', transform: p ? 'translateY(2px) scale(var(--press-scale))' : 'none', transition: 'transform var(--dur-fast) var(--ease-out)', ...style }}>
      <Icon name={icon} size={Math.round(size * 0.52)} />
    </button>
  );
}
