import React from 'react';
import { Icon } from './Icon.jsx';
const SIZES = { sm: { h: 40, px: 16, fs: 15, ic: 20 }, md: { h: 52, px: 22, fs: 18, ic: 24 }, lg: { h: 68, px: 32, fs: 24, ic: 30 } };
const VARIANTS = {
  primary: { bg: 'var(--accent)', bgP: 'var(--accent-pressed)', fg: 'var(--text-on-accent)', bd: 'var(--border-strong)', sh: true },
  secondary: { bg: 'var(--surface-card)', bgP: 'var(--beige-100)', fg: 'var(--text-strong)', bd: 'var(--border-strong)', sh: true },
  mangrove: { bg: 'var(--secondary)', bgP: 'var(--manglar-800)', fg: 'var(--crema-50)', bd: 'var(--cafe-950)', sh: true },
  dark: { bg: 'var(--cafe-900)', bgP: 'var(--cafe-950)', fg: 'var(--platano-500)', bd: 'var(--cafe-950)', sh: true },
  ghost: { bg: 'transparent', bgP: 'rgba(43,30,22,.08)', fg: 'var(--text-strong)', bd: 'transparent', sh: false },
};
export function Button({ children, variant = 'primary', size = 'md', icon, iconRight, disabled = false, fullWidth = false, onClick, style }) {
  const [down, setDown] = React.useState(false);
  const s = SIZES[size] || SIZES.md, v = VARIANTS[variant] || VARIANTS.primary;
  const pressed = down && !disabled;
  return (
    <button type="button" disabled={disabled} onClick={onClick}
      onPointerDown={() => setDown(true)} onPointerUp={() => setDown(false)} onPointerLeave={() => setDown(false)}
      style={{ display: fullWidth ? 'flex' : 'inline-flex', width: fullWidth ? '100%' : undefined, alignItems: 'center', justifyContent: 'center', gap: 10,
        height: s.h, padding: `0 ${s.px}px`, borderRadius: 'var(--radius-md)', border: `var(--border-w) solid ${disabled ? 'var(--beige-300)' : v.bd}`,
        background: disabled ? 'var(--disabled-bg)' : pressed ? v.bgP : v.bg, color: disabled ? 'var(--disabled-fg)' : v.fg,
        font: `700 ${s.fs}px/1 var(--font-display)`, letterSpacing: '.01em', cursor: disabled ? 'not-allowed' : 'pointer',
        boxShadow: disabled || !v.sh ? 'none' : pressed ? 'var(--shadow-pop-sm)' : 'var(--shadow-pop)',
        transform: pressed ? 'translateY(2px) scale(var(--press-scale))' : 'none', transition: 'transform var(--dur-fast) var(--ease-out), box-shadow var(--dur-fast), background var(--dur-fast)', ...style }}>
      {icon && <Icon name={icon} size={s.ic} />}{children}{iconRight && <Icon name={iconRight} size={s.ic} />}
    </button>
  );
}
