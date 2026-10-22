import React from 'react';
import { BackButton } from '../core/BackButton.jsx';
import { IconButton } from '../core/IconButton.jsx';
import { Tag } from '../core/Tag.jsx';
export function TopBar({ title, onBack, rightIcon = 'info', rightLabel = 'Información', onRight, badge, style }) {
  return (
    <div style={{ display: 'grid', gridTemplateColumns: '1fr auto 1fr', alignItems: 'center', gap: 8, minHeight: 64, padding: '10px 14px', background: 'var(--bg-topbar)', borderBottom: 'var(--border-w) solid var(--border-strong)', ...style }}>
      <div>{onBack && <BackButton onClick={onBack} />}</div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, minWidth: 0 }}>
        <span style={{ font: '800 19px/1.1 var(--font-display)', color: 'var(--text-strong)', textAlign: 'center', textWrap: 'balance' }}>{title}</span>
        {badge && <Tag tone="accent">{badge}</Tag>}
      </div>
      <div style={{ justifySelf: 'end' }}>{rightIcon && <IconButton icon={rightIcon} label={rightLabel} size={40} onClick={onRight} />}</div>
    </div>
  );
}
