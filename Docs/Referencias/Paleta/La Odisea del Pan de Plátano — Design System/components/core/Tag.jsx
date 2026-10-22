import React from 'react';
import { Icon } from './Icon.jsx';
const T = { ally: ['var(--manglar-100)', 'var(--manglar-800)'], ingredient: ['var(--manglar-600)', 'var(--crema-50)'], rival: ['var(--chile-100)', 'var(--chile-500)'], neutral: ['var(--beige-200)', 'var(--cafe-800)'], accent: ['var(--platano-500)', 'var(--cafe-900)'], dark: ['var(--cafe-900)', 'var(--platano-500)'] };
export function Tag({ children, tone = 'neutral', icon, style }) {
  const [bg, fg] = T[tone] || T.neutral;
  return <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, height: 26, padding: icon ? '0 10px 0 7px' : '0 10px', borderRadius: 'var(--radius-pill)', background: bg, color: fg, font: '700 12px/1 var(--font-body)', letterSpacing: '.06em', textTransform: 'uppercase', whiteSpace: 'nowrap', ...style }}>{icon && <Icon name={icon} size={16} />}{children}</span>;
}
