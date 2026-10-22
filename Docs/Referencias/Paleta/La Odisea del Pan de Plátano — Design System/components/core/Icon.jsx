import React from 'react';
export function Icon({ name, size = 24, fill = true, weight = 600, color = 'currentColor', style }) {
  return <span aria-hidden="true" style={{ fontFamily: 'var(--font-icon)', fontSize: size, lineHeight: 1, width: size, height: size, display: 'inline-flex', alignItems: 'center', justifyContent: 'center', color, fontVariationSettings: `"FILL" ${fill ? 1 : 0}, "wght" ${weight}, "GRAD" 0, "opsz" ${Math.min(48, Math.max(20, size))}`, userSelect: 'none', flex: 'none', ...style }}>{name}</span>;
}
