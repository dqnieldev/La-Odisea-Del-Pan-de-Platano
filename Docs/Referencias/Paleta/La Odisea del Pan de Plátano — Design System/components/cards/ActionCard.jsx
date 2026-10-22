import React from 'react';
import { Icon } from '../core/Icon.jsx';
export function ActionCard({ title, subtitle, icon, locked = false, onClick, style }) {
  const [down, setDown] = React.useState(false);
  const p = down && !locked;
  return (
    <button type="button" disabled={locked} onClick={onClick} onPointerDown={() => setDown(true)} onPointerUp={() => setDown(false)} onPointerLeave={() => setDown(false)}
      style={{ display: 'flex', alignItems: 'center', gap: 16, width: '100%', textAlign: 'left', padding: '16px 16px 16px 18px', borderRadius: 'var(--radius-lg)',
        background: locked ? 'var(--disabled-bg)' : p ? 'var(--beige-100)' : 'var(--surface-card)', border: `var(--border-w) solid ${locked ? 'var(--beige-300)' : 'var(--border-strong)'}`,
        boxShadow: locked ? 'none' : p ? 'var(--shadow-pop-sm)' : 'var(--shadow-pop)', transform: p ? 'translateY(2px) scale(.985)' : 'none', transition: 'transform var(--dur-fast) var(--ease-out)', cursor: locked ? 'not-allowed' : 'pointer', ...style }}>
      {icon && <span style={{ width: 52, height: 52, borderRadius: 'var(--radius-md)', background: locked ? 'var(--beige-300)' : 'var(--cafe-900)', color: locked ? 'var(--cafe-500)' : 'var(--platano-500)', display: 'grid', placeItems: 'center', flex: 'none' }}><Icon name={icon} size={30} /></span>}
      <span style={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column', gap: 4 }}>
        <span style={{ font: '700 20px/1.15 var(--font-display)', color: locked ? 'var(--cafe-500)' : 'var(--text-strong)' }}>{title}</span>
        {subtitle && <span style={{ font: '400 14px/1.35 var(--font-body)', color: locked ? 'var(--cafe-500)' : 'var(--text-muted)' }}>{subtitle}</span>}
      </span>
      <Icon name={locked ? 'lock' : 'arrow_forward'} size={30} color={locked ? 'var(--cafe-500)' : 'var(--platano-500)'} weight={700} />
    </button>
  );
}
