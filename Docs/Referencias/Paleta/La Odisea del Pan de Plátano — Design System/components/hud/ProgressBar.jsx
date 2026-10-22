import React from 'react';
import { Icon } from '../core/Icon.jsx';
const TONES = { cook: 'var(--hud-cook)', banana: 'var(--platano-500)', mangrove: 'var(--manglar-500)', danger: 'var(--chile-500)' };
export function ProgressBar({ value = 0, label, icon, tone = 'cook', height = 22, showValue = true, style }) {
  const pct = Math.max(0, Math.min(100, value));
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 10, ...style }}>
      {icon && <span style={{ width: height + 18, height: height + 18, borderRadius: '50%', background: 'var(--cafe-900)', border: '2px solid var(--cafe-950)', display: 'grid', placeItems: 'center', flex: 'none' }}><Icon name={icon} size={height} color="var(--platano-500)" /></span>}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: 4 }}>
        {label && <span style={{ font: '700 12px/1 var(--font-body)', letterSpacing: '.06em', textTransform: 'uppercase', color: 'inherit' }}>{label}</span>}
        <div style={{ position: 'relative', height, borderRadius: 999, background: 'var(--cafe-800)', border: '2px solid var(--cafe-950)', overflow: 'hidden', boxShadow: 'inset 0 2px 0 rgba(0,0,0,.25)' }}>
          <div style={{ width: pct + '%', height: '100%', background: TONES[tone] || tone, borderRadius: 999, boxShadow: 'inset 0 -4px 0 rgba(0,0,0,.18), inset 0 3px 0 rgba(255,255,255,.3)', transition: 'width var(--dur-slow) var(--ease-out)' }}></div>
          {showValue && <span style={{ position: 'absolute', inset: 0, display: 'grid', placeItems: 'center', font: '800 12px/1 var(--font-display)', color: 'var(--crema-50)', textShadow: '0 1px 0 var(--cafe-950)' }}>{Math.round(pct)}%</span>}
        </div>
      </div>
    </div>
  );
}
