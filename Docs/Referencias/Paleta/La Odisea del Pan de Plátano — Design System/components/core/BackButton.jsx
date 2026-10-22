import React from 'react';
import { Icon } from './Icon.jsx';
export function BackButton({ label = 'Volver', onClick, onDark = false, style }) {
  const [down, setDown] = React.useState(false);
  return (
    <button type="button" onClick={onClick} onPointerDown={() => setDown(true)} onPointerUp={() => setDown(false)} onPointerLeave={() => setDown(false)}
      style={{ display: 'inline-flex', alignItems: 'center', gap: 4, height: 40, padding: '0 14px 0 8px', borderRadius: 'var(--radius-pill)',
        border: `var(--border-w) solid ${onDark ? 'var(--platano-500)' : 'var(--border-strong)'}`, background: down ? (onDark ? 'var(--cafe-950)' : 'var(--beige-200)') : (onDark ? 'var(--cafe-900)' : 'var(--surface-card)'),
        color: onDark ? 'var(--platano-500)' : 'var(--text-strong)', font: '600 15px/1 var(--font-body)', cursor: 'pointer', transform: down ? 'scale(var(--press-scale))' : 'none', transition: 'transform var(--dur-fast) var(--ease-out)', ...style }}>
      <Icon name="arrow_back" size={20} />{label}
    </button>
  );
}
