import React from 'react';
import { Icon } from '../core/Icon.jsx';
export function HeartMeter({ lives = 3, max = 3, size = 36, style }) {
  return <div role="img" aria-label={`${lives} de ${max} vidas`} style={{ display: 'inline-flex', gap: 4, ...style }}>{Array.from({ length: max }).map((_, i) => <Icon key={i} name="favorite" size={size} fill={i < lives} weight={700} color={i < lives ? 'var(--hud-heart)' : 'var(--cafe-500)'} style={{ filter: i < lives ? 'drop-shadow(0 2px 0 var(--cafe-950))' : 'none' }} />)}</div>;
}
