import React from 'react';
import { Tag } from '../core/Tag.jsx';
export function CharacterCard({ name, role, image, tone = 'ally', tagLabel, description, compact = false, onClick, style }) {
  return (
    <div onClick={onClick} style={{ background: 'var(--surface-card)', border: 'var(--border-w) solid var(--border-strong)', borderRadius: 'var(--radius-lg)', overflow: 'hidden', boxShadow: 'var(--shadow-pop)', cursor: onClick ? 'pointer' : 'default', display: 'flex', flexDirection: 'column', ...style }}>
      <div style={{ aspectRatio: compact ? '4/3' : '16/9', background: 'var(--beige-200)', borderBottom: 'var(--border-w) solid var(--border-strong)', overflow: 'hidden' }}>
        {image && <img src={image} alt={name} style={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }} />}
      </div>
      <div style={{ padding: compact ? 12 : 16, display: 'flex', flexDirection: 'column', gap: 6 }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 8 }}>
          <span style={{ font: `700 ${compact ? 17 : 20}px/1.15 var(--font-display)`, color: 'var(--text-strong)' }}>{name}</span>
          {tagLabel && <Tag tone={tone}>{tagLabel}</Tag>}
        </div>
        {role && <span style={{ font: 'italic 400 14px/1.3 var(--font-body)', color: 'var(--text-muted)' }}>{role}</span>}
        {description && !compact && <p style={{ margin: 0, font: '400 14px/1.5 var(--font-body)', color: 'var(--text-body)', textWrap: 'pretty' }}>{description}</p>}
      </div>
    </div>
  );
}
