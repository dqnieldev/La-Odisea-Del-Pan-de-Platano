import React from 'react';
import { Icon } from '../core/Icon.jsx';
export function ItemSlot({ icon, image, label, count, collected = true, active = false, size = 64, style }) {
  return (
    <div style={{ display: 'inline-flex', flexDirection: 'column', alignItems: 'center', gap: 6, ...style }}>
      <div style={{ position: 'relative', width: size, height: size, borderRadius: 'var(--radius-md)', display: 'grid', placeItems: 'center',
        background: collected ? 'var(--crema-50)' : 'var(--cafe-800)', border: `var(--border-w-thick) solid ${active ? 'var(--platano-500)' : 'var(--cafe-950)'}`,
        boxShadow: active ? '0 0 0 3px var(--cafe-950), var(--shadow-pop)' : 'var(--shadow-pop)' }}>
        {image ? <img src={image} alt={label} style={{ width: '78%', height: '78%', objectFit: 'contain', opacity: collected ? 1 : .35, filter: collected ? 'none' : 'grayscale(1)' }} />
          : <Icon name={icon} size={Math.round(size * .55)} color={collected ? 'var(--cafe-900)' : 'var(--cafe-500)'} />}
        {count != null && <span style={{ position: 'absolute', right: -8, bottom: -8, minWidth: 24, height: 24, padding: '0 6px', boxSizing: 'border-box', borderRadius: 999, background: 'var(--platano-500)', border: '2px solid var(--cafe-950)', font: '800 13px/20px var(--font-display)', color: 'var(--cafe-900)', textAlign: 'center' }}>{count}</span>}
      </div>
      {label && <span style={{ font: '600 12px/1 var(--font-body)', color: 'inherit' }}>{label}</span>}
    </div>
  );
}
