/* @ds-bundle: {"format":4,"namespace":"LaOdiseaDelPanDePlTanoDesignSystem_cce664","components":[{"name":"ActionCard","sourcePath":"components/cards/ActionCard.jsx"},{"name":"CharacterCard","sourcePath":"components/cards/CharacterCard.jsx"},{"name":"BackButton","sourcePath":"components/core/BackButton.jsx"},{"name":"Badge","sourcePath":"components/core/Badge.jsx"},{"name":"Button","sourcePath":"components/core/Button.jsx"},{"name":"Icon","sourcePath":"components/core/Icon.jsx"},{"name":"IconButton","sourcePath":"components/core/IconButton.jsx"},{"name":"Tag","sourcePath":"components/core/Tag.jsx"},{"name":"GamePanel","sourcePath":"components/feedback/GamePanel.jsx"},{"name":"HeartMeter","sourcePath":"components/hud/HeartMeter.jsx"},{"name":"HudChip","sourcePath":"components/hud/HudChip.jsx"},{"name":"ItemSlot","sourcePath":"components/hud/ItemSlot.jsx"},{"name":"ProgressBar","sourcePath":"components/hud/ProgressBar.jsx"},{"name":"TopBar","sourcePath":"components/navigation/TopBar.jsx"}],"sourceHashes":{"components/cards/ActionCard.jsx":"7cf7538a83d4","components/cards/CharacterCard.jsx":"0e60c07bf258","components/core/BackButton.jsx":"f332b3d8e527","components/core/Badge.jsx":"04c09a16d543","components/core/Button.jsx":"08bb1fbb50ad","components/core/Icon.jsx":"aa621b843d2e","components/core/IconButton.jsx":"e50161be6c19","components/core/Tag.jsx":"75bf69ab07de","components/feedback/GamePanel.jsx":"5662fd926845","components/hud/HeartMeter.jsx":"ffcaa4b7215a","components/hud/HudChip.jsx":"486426863052","components/hud/ItemSlot.jsx":"55aaee492eab","components/hud/ProgressBar.jsx":"886fda6613a2","components/navigation/TopBar.jsx":"a2b6e99e0bc5","ui_kits/companion/Screens.jsx":"d8e7d82dde62","ui_kits/companion/data.jsx":"eb48da1d1daf","ui_kits/juego/Level.jsx":"739780d64f14","ui_kits/juego/Menu.jsx":"c8e74b0d61a9","ui_kits/juego/WorldMap.jsx":"8c81a1570c10"},"inlinedExternals":[],"unexposedExports":[]} */

(() => {

const __ds_ns = (window.LaOdiseaDelPanDePlTanoDesignSystem_cce664 = window.LaOdiseaDelPanDePlTanoDesignSystem_cce664 || {});

const __ds_scope = {};

(__ds_ns.__errors = __ds_ns.__errors || []);

// components/core/Badge.jsx
try { (() => {
function Badge({
  children,
  tone = 'accent',
  style
}) {
  const c = {
    accent: ['var(--platano-500)', 'var(--cafe-900)'],
    danger: ['var(--chile-500)', '#fff'],
    dark: ['var(--cafe-900)', 'var(--platano-500)']
  }[tone] || [];
  return /*#__PURE__*/React.createElement("span", {
    style: {
      minWidth: 24,
      height: 24,
      padding: '0 7px',
      borderRadius: 999,
      display: 'inline-flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: c[0],
      color: c[1],
      border: '2px solid var(--cafe-900)',
      font: '800 13px/1 var(--font-display)',
      boxSizing: 'border-box',
      ...style
    }
  }, children);
}
Object.assign(__ds_scope, { Badge });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Badge.jsx", error: String((e && e.message) || e) }); }

// components/core/Icon.jsx
try { (() => {
function Icon({
  name,
  size = 24,
  fill = true,
  weight = 600,
  color = 'currentColor',
  style
}) {
  return /*#__PURE__*/React.createElement("span", {
    "aria-hidden": "true",
    style: {
      fontFamily: 'var(--font-icon)',
      fontSize: size,
      lineHeight: 1,
      width: size,
      height: size,
      display: 'inline-flex',
      alignItems: 'center',
      justifyContent: 'center',
      color,
      fontVariationSettings: `"FILL" ${fill ? 1 : 0}, "wght" ${weight}, "GRAD" 0, "opsz" ${Math.min(48, Math.max(20, size))}`,
      userSelect: 'none',
      flex: 'none',
      ...style
    }
  }, name);
}
Object.assign(__ds_scope, { Icon });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Icon.jsx", error: String((e && e.message) || e) }); }

// components/cards/ActionCard.jsx
try { (() => {
function ActionCard({
  title,
  subtitle,
  icon,
  locked = false,
  onClick,
  style
}) {
  const [down, setDown] = React.useState(false);
  const p = down && !locked;
  return /*#__PURE__*/React.createElement("button", {
    type: "button",
    disabled: locked,
    onClick: onClick,
    onPointerDown: () => setDown(true),
    onPointerUp: () => setDown(false),
    onPointerLeave: () => setDown(false),
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 16,
      width: '100%',
      textAlign: 'left',
      padding: '16px 16px 16px 18px',
      borderRadius: 'var(--radius-lg)',
      background: locked ? 'var(--disabled-bg)' : p ? 'var(--beige-100)' : 'var(--surface-card)',
      border: `var(--border-w) solid ${locked ? 'var(--beige-300)' : 'var(--border-strong)'}`,
      boxShadow: locked ? 'none' : p ? 'var(--shadow-pop-sm)' : 'var(--shadow-pop)',
      transform: p ? 'translateY(2px) scale(.985)' : 'none',
      transition: 'transform var(--dur-fast) var(--ease-out)',
      cursor: locked ? 'not-allowed' : 'pointer',
      ...style
    }
  }, icon && /*#__PURE__*/React.createElement("span", {
    style: {
      width: 52,
      height: 52,
      borderRadius: 'var(--radius-md)',
      background: locked ? 'var(--beige-300)' : 'var(--cafe-900)',
      color: locked ? 'var(--cafe-500)' : 'var(--platano-500)',
      display: 'grid',
      placeItems: 'center',
      flex: 'none'
    }
  }, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: icon,
    size: 30
  })), /*#__PURE__*/React.createElement("span", {
    style: {
      flex: 1,
      minWidth: 0,
      display: 'flex',
      flexDirection: 'column',
      gap: 4
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      font: '700 20px/1.15 var(--font-display)',
      color: locked ? 'var(--cafe-500)' : 'var(--text-strong)'
    }
  }, title), subtitle && /*#__PURE__*/React.createElement("span", {
    style: {
      font: '400 14px/1.35 var(--font-body)',
      color: locked ? 'var(--cafe-500)' : 'var(--text-muted)'
    }
  }, subtitle)), /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: locked ? 'lock' : 'arrow_forward',
    size: 30,
    color: locked ? 'var(--cafe-500)' : 'var(--platano-500)',
    weight: 700
  }));
}
Object.assign(__ds_scope, { ActionCard });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/cards/ActionCard.jsx", error: String((e && e.message) || e) }); }

// components/core/BackButton.jsx
try { (() => {
function BackButton({
  label = 'Volver',
  onClick,
  onDark = false,
  style
}) {
  const [down, setDown] = React.useState(false);
  return /*#__PURE__*/React.createElement("button", {
    type: "button",
    onClick: onClick,
    onPointerDown: () => setDown(true),
    onPointerUp: () => setDown(false),
    onPointerLeave: () => setDown(false),
    style: {
      display: 'inline-flex',
      alignItems: 'center',
      gap: 4,
      height: 40,
      padding: '0 14px 0 8px',
      borderRadius: 'var(--radius-pill)',
      border: `var(--border-w) solid ${onDark ? 'var(--platano-500)' : 'var(--border-strong)'}`,
      background: down ? onDark ? 'var(--cafe-950)' : 'var(--beige-200)' : onDark ? 'var(--cafe-900)' : 'var(--surface-card)',
      color: onDark ? 'var(--platano-500)' : 'var(--text-strong)',
      font: '600 15px/1 var(--font-body)',
      cursor: 'pointer',
      transform: down ? 'scale(var(--press-scale))' : 'none',
      transition: 'transform var(--dur-fast) var(--ease-out)',
      ...style
    }
  }, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: "arrow_back",
    size: 20
  }), label);
}
Object.assign(__ds_scope, { BackButton });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/BackButton.jsx", error: String((e && e.message) || e) }); }

// components/core/Button.jsx
try { (() => {
const SIZES = {
  sm: {
    h: 40,
    px: 16,
    fs: 15,
    ic: 20
  },
  md: {
    h: 52,
    px: 22,
    fs: 18,
    ic: 24
  },
  lg: {
    h: 68,
    px: 32,
    fs: 24,
    ic: 30
  }
};
const VARIANTS = {
  primary: {
    bg: 'var(--accent)',
    bgP: 'var(--accent-pressed)',
    fg: 'var(--text-on-accent)',
    bd: 'var(--border-strong)',
    sh: true
  },
  secondary: {
    bg: 'var(--surface-card)',
    bgP: 'var(--beige-100)',
    fg: 'var(--text-strong)',
    bd: 'var(--border-strong)',
    sh: true
  },
  mangrove: {
    bg: 'var(--secondary)',
    bgP: 'var(--manglar-800)',
    fg: 'var(--crema-50)',
    bd: 'var(--cafe-950)',
    sh: true
  },
  dark: {
    bg: 'var(--cafe-900)',
    bgP: 'var(--cafe-950)',
    fg: 'var(--platano-500)',
    bd: 'var(--cafe-950)',
    sh: true
  },
  ghost: {
    bg: 'transparent',
    bgP: 'rgba(43,30,22,.08)',
    fg: 'var(--text-strong)',
    bd: 'transparent',
    sh: false
  }
};
function Button({
  children,
  variant = 'primary',
  size = 'md',
  icon,
  iconRight,
  disabled = false,
  fullWidth = false,
  onClick,
  style
}) {
  const [down, setDown] = React.useState(false);
  const s = SIZES[size] || SIZES.md,
    v = VARIANTS[variant] || VARIANTS.primary;
  const pressed = down && !disabled;
  return /*#__PURE__*/React.createElement("button", {
    type: "button",
    disabled: disabled,
    onClick: onClick,
    onPointerDown: () => setDown(true),
    onPointerUp: () => setDown(false),
    onPointerLeave: () => setDown(false),
    style: {
      display: fullWidth ? 'flex' : 'inline-flex',
      width: fullWidth ? '100%' : undefined,
      alignItems: 'center',
      justifyContent: 'center',
      gap: 10,
      height: s.h,
      padding: `0 ${s.px}px`,
      borderRadius: 'var(--radius-md)',
      border: `var(--border-w) solid ${disabled ? 'var(--beige-300)' : v.bd}`,
      background: disabled ? 'var(--disabled-bg)' : pressed ? v.bgP : v.bg,
      color: disabled ? 'var(--disabled-fg)' : v.fg,
      font: `700 ${s.fs}px/1 var(--font-display)`,
      letterSpacing: '.01em',
      cursor: disabled ? 'not-allowed' : 'pointer',
      boxShadow: disabled || !v.sh ? 'none' : pressed ? 'var(--shadow-pop-sm)' : 'var(--shadow-pop)',
      transform: pressed ? 'translateY(2px) scale(var(--press-scale))' : 'none',
      transition: 'transform var(--dur-fast) var(--ease-out), box-shadow var(--dur-fast), background var(--dur-fast)',
      ...style
    }
  }, icon && /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: icon,
    size: s.ic
  }), children, iconRight && /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: iconRight,
    size: s.ic
  }));
}
Object.assign(__ds_scope, { Button });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Button.jsx", error: String((e && e.message) || e) }); }

// components/core/IconButton.jsx
try { (() => {
function IconButton({
  icon,
  label,
  size = 48,
  variant = 'light',
  disabled = false,
  onClick,
  style
}) {
  const [down, setDown] = React.useState(false);
  const p = down && !disabled;
  const V = {
    light: ['var(--surface-card)', 'var(--beige-100)', 'var(--text-strong)'],
    accent: ['var(--accent)', 'var(--accent-pressed)', 'var(--cafe-900)'],
    dark: ['var(--cafe-900)', 'var(--cafe-950)', 'var(--platano-500)']
  }[variant] || [];
  return /*#__PURE__*/React.createElement("button", {
    type: "button",
    "aria-label": label,
    title: label,
    disabled: disabled,
    onClick: onClick,
    onPointerDown: () => setDown(true),
    onPointerUp: () => setDown(false),
    onPointerLeave: () => setDown(false),
    style: {
      width: size,
      height: size,
      borderRadius: '50%',
      display: 'inline-grid',
      placeItems: 'center',
      flex: 'none',
      border: `var(--border-w) solid ${disabled ? 'var(--beige-300)' : 'var(--border-strong)'}`,
      background: disabled ? 'var(--disabled-bg)' : p ? V[1] : V[0],
      color: disabled ? 'var(--disabled-fg)' : V[2],
      cursor: disabled ? 'not-allowed' : 'pointer',
      padding: 0,
      boxShadow: disabled ? 'none' : p ? 'var(--shadow-pop-sm)' : 'var(--shadow-pop)',
      transform: p ? 'translateY(2px) scale(var(--press-scale))' : 'none',
      transition: 'transform var(--dur-fast) var(--ease-out)',
      ...style
    }
  }, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: icon,
    size: Math.round(size * 0.52)
  }));
}
Object.assign(__ds_scope, { IconButton });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/IconButton.jsx", error: String((e && e.message) || e) }); }

// components/core/Tag.jsx
try { (() => {
const T = {
  ally: ['var(--manglar-100)', 'var(--manglar-800)'],
  ingredient: ['var(--manglar-600)', 'var(--crema-50)'],
  rival: ['var(--chile-100)', 'var(--chile-500)'],
  neutral: ['var(--beige-200)', 'var(--cafe-800)'],
  accent: ['var(--platano-500)', 'var(--cafe-900)'],
  dark: ['var(--cafe-900)', 'var(--platano-500)']
};
function Tag({
  children,
  tone = 'neutral',
  icon,
  style
}) {
  const [bg, fg] = T[tone] || T.neutral;
  return /*#__PURE__*/React.createElement("span", {
    style: {
      display: 'inline-flex',
      alignItems: 'center',
      gap: 4,
      height: 26,
      padding: icon ? '0 10px 0 7px' : '0 10px',
      borderRadius: 'var(--radius-pill)',
      background: bg,
      color: fg,
      font: '700 12px/1 var(--font-body)',
      letterSpacing: '.06em',
      textTransform: 'uppercase',
      whiteSpace: 'nowrap',
      ...style
    }
  }, icon && /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: icon,
    size: 16
  }), children);
}
Object.assign(__ds_scope, { Tag });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Tag.jsx", error: String((e && e.message) || e) }); }

// components/cards/CharacterCard.jsx
try { (() => {
function CharacterCard({
  name,
  role,
  image,
  tone = 'ally',
  tagLabel,
  description,
  compact = false,
  onClick,
  style
}) {
  return /*#__PURE__*/React.createElement("div", {
    onClick: onClick,
    style: {
      background: 'var(--surface-card)',
      border: 'var(--border-w) solid var(--border-strong)',
      borderRadius: 'var(--radius-lg)',
      overflow: 'hidden',
      boxShadow: 'var(--shadow-pop)',
      cursor: onClick ? 'pointer' : 'default',
      display: 'flex',
      flexDirection: 'column',
      ...style
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      aspectRatio: compact ? '4/3' : '16/9',
      background: 'var(--beige-200)',
      borderBottom: 'var(--border-w) solid var(--border-strong)',
      overflow: 'hidden'
    }
  }, image && /*#__PURE__*/React.createElement("img", {
    src: image,
    alt: name,
    style: {
      width: '100%',
      height: '100%',
      objectFit: 'cover',
      display: 'block'
    }
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      padding: compact ? 12 : 16,
      display: 'flex',
      flexDirection: 'column',
      gap: 6
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      gap: 8
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      font: `700 ${compact ? 17 : 20}px/1.15 var(--font-display)`,
      color: 'var(--text-strong)'
    }
  }, name), tagLabel && /*#__PURE__*/React.createElement(__ds_scope.Tag, {
    tone: tone
  }, tagLabel)), role && /*#__PURE__*/React.createElement("span", {
    style: {
      font: 'italic 400 14px/1.3 var(--font-body)',
      color: 'var(--text-muted)'
    }
  }, role), description && !compact && /*#__PURE__*/React.createElement("p", {
    style: {
      margin: 0,
      font: '400 14px/1.5 var(--font-body)',
      color: 'var(--text-body)',
      textWrap: 'pretty'
    }
  }, description)));
}
Object.assign(__ds_scope, { CharacterCard });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/cards/CharacterCard.jsx", error: String((e && e.message) || e) }); }

// components/feedback/GamePanel.jsx
try { (() => {
function GamePanel({
  title,
  eyebrow,
  tone = 'light',
  children,
  actions,
  width = 520,
  style
}) {
  const dark = tone === 'dark';
  return /*#__PURE__*/React.createElement("div", {
    role: "dialog",
    "aria-label": title,
    style: {
      width,
      maxWidth: '100%',
      boxSizing: 'border-box',
      borderRadius: 'var(--radius-xl)',
      padding: '28px 28px 24px',
      display: 'flex',
      flexDirection: 'column',
      gap: 18,
      background: dark ? 'var(--cafe-900)' : 'var(--crema-50)',
      color: dark ? 'var(--crema-50)' : 'var(--text-body)',
      border: `var(--border-w-thick) solid var(--cafe-950)`,
      boxShadow: 'var(--shadow-pop-lg), var(--shadow-float)',
      ...style
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 6,
      alignItems: 'center',
      textAlign: 'center'
    }
  }, eyebrow && /*#__PURE__*/React.createElement("span", {
    style: {
      font: '700 12px/1 var(--font-body)',
      letterSpacing: '.08em',
      textTransform: 'uppercase',
      color: dark ? 'var(--cafe-300)' : 'var(--text-muted)'
    }
  }, eyebrow), /*#__PURE__*/React.createElement("span", {
    style: {
      font: '800 40px/1.05 var(--font-display)',
      color: dark ? 'var(--platano-500)' : 'var(--text-strong)',
      textWrap: 'balance'
    }
  }, title)), children, actions && /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 12
    }
  }, actions));
}
Object.assign(__ds_scope, { GamePanel });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/GamePanel.jsx", error: String((e && e.message) || e) }); }

// components/hud/HeartMeter.jsx
try { (() => {
function HeartMeter({
  lives = 3,
  max = 3,
  size = 36,
  style
}) {
  return /*#__PURE__*/React.createElement("div", {
    role: "img",
    "aria-label": `${lives} de ${max} vidas`,
    style: {
      display: 'inline-flex',
      gap: 4,
      ...style
    }
  }, Array.from({
    length: max
  }).map((_, i) => /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    key: i,
    name: "favorite",
    size: size,
    fill: i < lives,
    weight: 700,
    color: i < lives ? 'var(--hud-heart)' : 'var(--cafe-500)',
    style: {
      filter: i < lives ? 'drop-shadow(0 2px 0 var(--cafe-950))' : 'none'
    }
  })));
}
Object.assign(__ds_scope, { HeartMeter });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/hud/HeartMeter.jsx", error: String((e && e.message) || e) }); }

// components/hud/HudChip.jsx
try { (() => {
function HudChip({
  icon,
  value,
  label,
  accent = false,
  style
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'inline-flex',
      alignItems: 'center',
      gap: 10,
      height: 52,
      padding: '0 18px 0 12px',
      borderRadius: 'var(--radius-pill)',
      background: 'var(--cafe-900)',
      border: 'var(--border-w-thick) solid var(--cafe-950)',
      boxShadow: 'var(--shadow-pop)',
      color: accent ? 'var(--platano-500)' : 'var(--crema-50)',
      ...style
    }
  }, icon && /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: icon,
    size: 30,
    color: "var(--platano-500)"
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 2
    }
  }, label && /*#__PURE__*/React.createElement("span", {
    style: {
      font: '700 10px/1 var(--font-body)',
      letterSpacing: '.08em',
      textTransform: 'uppercase',
      color: 'var(--cafe-300)'
    }
  }, label), /*#__PURE__*/React.createElement("span", {
    style: {
      font: '800 24px/1 var(--font-display)',
      fontVariantNumeric: 'tabular-nums'
    }
  }, value)));
}
Object.assign(__ds_scope, { HudChip });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/hud/HudChip.jsx", error: String((e && e.message) || e) }); }

// components/hud/ItemSlot.jsx
try { (() => {
function ItemSlot({
  icon,
  image,
  label,
  count,
  collected = true,
  active = false,
  size = 64,
  style
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'inline-flex',
      flexDirection: 'column',
      alignItems: 'center',
      gap: 6,
      ...style
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'relative',
      width: size,
      height: size,
      borderRadius: 'var(--radius-md)',
      display: 'grid',
      placeItems: 'center',
      background: collected ? 'var(--crema-50)' : 'var(--cafe-800)',
      border: `var(--border-w-thick) solid ${active ? 'var(--platano-500)' : 'var(--cafe-950)'}`,
      boxShadow: active ? '0 0 0 3px var(--cafe-950), var(--shadow-pop)' : 'var(--shadow-pop)'
    }
  }, image ? /*#__PURE__*/React.createElement("img", {
    src: image,
    alt: label,
    style: {
      width: '78%',
      height: '78%',
      objectFit: 'contain',
      opacity: collected ? 1 : .35,
      filter: collected ? 'none' : 'grayscale(1)'
    }
  }) : /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: icon,
    size: Math.round(size * .55),
    color: collected ? 'var(--cafe-900)' : 'var(--cafe-500)'
  }), count != null && /*#__PURE__*/React.createElement("span", {
    style: {
      position: 'absolute',
      right: -8,
      bottom: -8,
      minWidth: 24,
      height: 24,
      padding: '0 6px',
      boxSizing: 'border-box',
      borderRadius: 999,
      background: 'var(--platano-500)',
      border: '2px solid var(--cafe-950)',
      font: '800 13px/20px var(--font-display)',
      color: 'var(--cafe-900)',
      textAlign: 'center'
    }
  }, count)), label && /*#__PURE__*/React.createElement("span", {
    style: {
      font: '600 12px/1 var(--font-body)',
      color: 'inherit'
    }
  }, label));
}
Object.assign(__ds_scope, { ItemSlot });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/hud/ItemSlot.jsx", error: String((e && e.message) || e) }); }

// components/hud/ProgressBar.jsx
try { (() => {
const TONES = {
  cook: 'var(--hud-cook)',
  banana: 'var(--platano-500)',
  mangrove: 'var(--manglar-500)',
  danger: 'var(--chile-500)'
};
function ProgressBar({
  value = 0,
  label,
  icon,
  tone = 'cook',
  height = 22,
  showValue = true,
  style
}) {
  const pct = Math.max(0, Math.min(100, value));
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 10,
      ...style
    }
  }, icon && /*#__PURE__*/React.createElement("span", {
    style: {
      width: height + 18,
      height: height + 18,
      borderRadius: '50%',
      background: 'var(--cafe-900)',
      border: '2px solid var(--cafe-950)',
      display: 'grid',
      placeItems: 'center',
      flex: 'none'
    }
  }, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: icon,
    size: height,
    color: "var(--platano-500)"
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      display: 'flex',
      flexDirection: 'column',
      gap: 4
    }
  }, label && /*#__PURE__*/React.createElement("span", {
    style: {
      font: '700 12px/1 var(--font-body)',
      letterSpacing: '.06em',
      textTransform: 'uppercase',
      color: 'inherit'
    }
  }, label), /*#__PURE__*/React.createElement("div", {
    style: {
      position: 'relative',
      height,
      borderRadius: 999,
      background: 'var(--cafe-800)',
      border: '2px solid var(--cafe-950)',
      overflow: 'hidden',
      boxShadow: 'inset 0 2px 0 rgba(0,0,0,.25)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      width: pct + '%',
      height: '100%',
      background: TONES[tone] || tone,
      borderRadius: 999,
      boxShadow: 'inset 0 -4px 0 rgba(0,0,0,.18), inset 0 3px 0 rgba(255,255,255,.3)',
      transition: 'width var(--dur-slow) var(--ease-out)'
    }
  }), showValue && /*#__PURE__*/React.createElement("span", {
    style: {
      position: 'absolute',
      inset: 0,
      display: 'grid',
      placeItems: 'center',
      font: '800 12px/1 var(--font-display)',
      color: 'var(--crema-50)',
      textShadow: '0 1px 0 var(--cafe-950)'
    }
  }, Math.round(pct), "%"))));
}
Object.assign(__ds_scope, { ProgressBar });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/hud/ProgressBar.jsx", error: String((e && e.message) || e) }); }

// components/navigation/TopBar.jsx
try { (() => {
function TopBar({
  title,
  onBack,
  rightIcon = 'info',
  rightLabel = 'Información',
  onRight,
  badge,
  style
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr auto 1fr',
      alignItems: 'center',
      gap: 8,
      minHeight: 64,
      padding: '10px 14px',
      background: 'var(--bg-topbar)',
      borderBottom: 'var(--border-w) solid var(--border-strong)',
      ...style
    }
  }, /*#__PURE__*/React.createElement("div", null, onBack && /*#__PURE__*/React.createElement(__ds_scope.BackButton, {
    onClick: onBack
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 8,
      minWidth: 0
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      font: '800 19px/1.1 var(--font-display)',
      color: 'var(--text-strong)',
      textAlign: 'center',
      textWrap: 'balance'
    }
  }, title), badge && /*#__PURE__*/React.createElement(__ds_scope.Tag, {
    tone: "accent"
  }, badge)), /*#__PURE__*/React.createElement("div", {
    style: {
      justifySelf: 'end'
    }
  }, rightIcon && /*#__PURE__*/React.createElement(__ds_scope.IconButton, {
    icon: rightIcon,
    label: rightLabel,
    size: 40,
    onClick: onRight
  })));
}
Object.assign(__ds_scope, { TopBar });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/navigation/TopBar.jsx", error: String((e && e.message) || e) }); }

// ui_kits/companion/Screens.jsx
try { (() => {
(() => {
  const {
    TopBar,
    ActionCard,
    CharacterCard,
    Tag,
    Icon
  } = window.LaOdiseaDelPanDePlTanoDesignSystem_cce664;
  const D = window.ODISEA;
  const Body = ({
    children,
    pad = 16
  }) => /*#__PURE__*/React.createElement("div", {
    style: {
      flex: 1,
      overflowY: 'auto',
      padding: `${pad}px ${pad}px 40px`,
      display: 'flex',
      flexDirection: 'column',
      gap: 14
    }
  }, children);
  function HomeScreen({
    go
  }) {
    return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(TopBar, {
      title: "La Odisea del Pan de Pl\xE1tano",
      badge: "Wiki",
      rightIcon: "info"
    }), /*#__PURE__*/React.createElement(Body, null, /*#__PURE__*/React.createElement("div", {
      style: {
        borderRadius: 'var(--radius-lg)',
        overflow: 'hidden',
        border: '2px solid var(--cafe-900)',
        boxShadow: 'var(--shadow-pop)',
        position: 'relative',
        height: 170,
        flex: 'none'
      }
    }, /*#__PURE__*/React.createElement("img", {
      src: "../../assets/illustrations/mapa-ruta-del-pan.png",
      style: {
        width: '100%',
        height: '100%',
        objectFit: 'cover'
      }
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        position: 'absolute',
        left: 0,
        right: 0,
        bottom: 0,
        padding: '28px 14px 12px',
        background: 'linear-gradient(transparent, rgba(28,19,14,.85))',
        font: '700 18px/1.2 var(--font-display)',
        color: 'var(--crema-50)'
      }
    }, "La Ruta del Pan \xB7 San Blas, Nayarit")), /*#__PURE__*/React.createElement(ActionCard, {
      icon: "auto_stories",
      title: "Ver Historia",
      subtitle: "Pr\xF3logo y tres actos",
      onClick: () => go('historia')
    }), /*#__PURE__*/React.createElement(ActionCard, {
      icon: "groups",
      title: "Personajes",
      subtitle: "H\xE9roes, aliados y rivales",
      onClick: () => go('personajes')
    }), /*#__PURE__*/React.createElement(ActionCard, {
      icon: "bakery_dining",
      title: "Receta",
      subtitle: "Ingredientes del pan definitivo",
      onClick: () => go('receta')
    }), /*#__PURE__*/React.createElement(ActionCard, {
      icon: "sports_esports",
      title: "Niveles",
      subtitle: "Disponible al terminar el Acto I",
      locked: true
    })));
  }
  function HistoriaScreen({
    back
  }) {
    const [open, setOpen] = React.useState(0);
    return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(TopBar, {
      title: "Historia",
      onBack: back
    }), /*#__PURE__*/React.createElement(Body, null, /*#__PURE__*/React.createElement("img", {
      src: "../../assets/illustrations/storyboard.png",
      style: {
        width: '100%',
        borderRadius: 'var(--radius-lg)',
        border: '2px solid var(--cafe-900)',
        flex: 'none'
      }
    }), D.actos.map((a, i) => /*#__PURE__*/React.createElement("div", {
      key: a.k,
      onClick: () => setOpen(open === i ? -1 : i),
      style: {
        background: 'var(--surface-card)',
        border: '2px solid var(--cafe-900)',
        borderRadius: 'var(--radius-lg)',
        padding: 14,
        boxShadow: 'var(--shadow-pop-sm)',
        cursor: 'pointer',
        display: 'flex',
        flexDirection: 'column',
        gap: 8
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        alignItems: 'center',
        gap: 10
      }
    }, /*#__PURE__*/React.createElement(Tag, {
      tone: i === 0 ? 'accent' : 'dark'
    }, a.k), /*#__PURE__*/React.createElement("span", {
      style: {
        flex: 1,
        font: '700 16px/1.2 var(--font-display)',
        color: 'var(--text-strong)'
      }
    }, a.t), /*#__PURE__*/React.createElement(Icon, {
      name: open === i ? 'expand_less' : 'expand_more'
    })), open === i && /*#__PURE__*/React.createElement("p", {
      style: {
        margin: 0,
        font: '400 15px/1.5 var(--font-body)',
        color: 'var(--text-body)'
      }
    }, a.s)))));
  }
  function PersonajesScreen({
    back,
    go
  }) {
    return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(TopBar, {
      title: "Personajes",
      onBack: back
    }), /*#__PURE__*/React.createElement(Body, null, /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'grid',
        gridTemplateColumns: '1fr 1fr',
        gap: 12
      }
    }, D.personajes.map(c => /*#__PURE__*/React.createElement(CharacterCard, {
      key: c.id,
      compact: true,
      name: c.name,
      role: c.role,
      tone: c.tone,
      image: c.img,
      onClick: () => go('personaje', c)
    })))));
  }
  function PersonajeScreen({
    back,
    p
  }) {
    return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(TopBar, {
      title: p.name,
      onBack: back
    }), /*#__PURE__*/React.createElement(Body, null, /*#__PURE__*/React.createElement(CharacterCard, {
      name: p.name,
      role: p.role,
      tone: p.tone,
      tagLabel: p.tag,
      image: p.img,
      description: p.d
    })));
  }
  function RecetaScreen({
    back
  }) {
    const [done, setDone] = React.useState({
      0: true,
      1: true
    });
    return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(TopBar, {
      title: "Receta",
      onBack: back
    }), /*#__PURE__*/React.createElement(Body, null, /*#__PURE__*/React.createElement("div", {
      style: {
        font: '800 26px/1.1 var(--font-display)',
        color: 'var(--text-strong)'
      }
    }, "El pan de pl\xE1tano definitivo"), /*#__PURE__*/React.createElement("p", {
      style: {
        margin: 0,
        font: '400 15px/1.5 var(--font-body)',
        color: 'var(--text-muted)'
      }
    }, "Sin atajos de supermercado: los frutos m\xE1s selectos de la costa."), D.receta.map((r, i) => /*#__PURE__*/React.createElement("div", {
      key: r.n,
      onClick: () => setDone({
        ...done,
        [i]: !done[i]
      }),
      style: {
        display: 'flex',
        alignItems: 'center',
        gap: 12,
        background: 'var(--surface-card)',
        border: '2px solid var(--cafe-900)',
        borderRadius: 'var(--radius-md)',
        padding: 10,
        cursor: 'pointer'
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        width: 44,
        height: 44,
        borderRadius: 'var(--radius-sm)',
        background: done[i] ? 'var(--manglar-600)' : 'var(--beige-200)',
        color: done[i] ? 'var(--crema-50)' : 'var(--cafe-700)',
        display: 'grid',
        placeItems: 'center'
      }
    }, /*#__PURE__*/React.createElement(Icon, {
      name: r.i
    })), /*#__PURE__*/React.createElement("span", {
      style: {
        flex: 1,
        display: 'flex',
        flexDirection: 'column',
        gap: 3
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        font: '600 16px var(--font-body)',
        color: 'var(--text-strong)'
      }
    }, r.n), /*#__PURE__*/React.createElement("span", {
      style: {
        font: '400 13px var(--font-body)',
        color: 'var(--text-muted)'
      }
    }, r.w)), done[i] ? /*#__PURE__*/React.createElement(Tag, {
      tone: "ally",
      icon: "check"
    }, "Tengo") : /*#__PURE__*/React.createElement(Tag, null, "Falta")))));
  }
  Object.assign(window, {
    HomeScreen,
    HistoriaScreen,
    PersonajesScreen,
    PersonajeScreen,
    RecetaScreen
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/companion/Screens.jsx", error: String((e && e.message) || e) }); }

// ui_kits/companion/data.jsx
try { (() => {
window.ODISEA = {
  actos: [{
    k: 'Prólogo',
    t: 'El Decreto del Viejo Horno',
    s: 'Don Lupe lanza el ultimátum: quien cree el pan de plátano definitivo y lo lleve a tiempo al Festival será Empleado del Mes.'
  }, {
    k: 'Acto I',
    t: 'La Cueva del Mercado y el Fruto Prohibido',
    s: 'Escalada vertical por el Mercado Municipal para arrancar los plátanos machos reales a Polifemo, mientras Circe arroja cáscaras.'
  }, {
    k: 'Acto II',
    t: 'El Asedio a la Panadería de Piedra',
    s: 'Ratas, jejenes, cocodrilos y ayudantes de Circe asedian el horno de barro mientras el pan sube.'
  }, {
    k: 'Acto III',
    t: 'Carrera Desesperada por los Manglares de La Tovara',
    s: 'Con la canasta a la espalda, Ulises cruza el manglar a contrarreloj hasta la carpa de los jueces.'
  }],
  personajes: [{
    id: 'ulises',
    name: 'Ulises',
    role: 'El Panadero',
    tone: 'ally',
    tag: 'Héroe',
    img: '../../assets/characters/ulises-sheet.png',
    d: 'Joven artesano de San Blas con un talento nato para las masas y una terquedad inquebrantable. Decidido a honrar la receta tradicional de su pueblo, no descansará hasta demostrar que el verdadero sabor nace del corazón y la leña.'
  }, {
    id: 'lupe',
    name: 'Don Lupe',
    role: 'El Maestro Panadero',
    tone: 'ally',
    tag: 'Aliado',
    img: '../../assets/characters/don-lupe-sheet.png',
    d: 'Veterano dueño de la panadería tradicional y guardián del horno de barro más antiguo del puerto. Cansado pero sabio, busca a un sucesor digno que mantenga viva la esencia culinaria de la costa.'
  }, {
    id: 'circe',
    name: 'Madame Circe',
    role: 'La Chef Rival',
    tone: 'rival',
    tag: 'Rival',
    img: '../../assets/characters/madame-circe-sheet.png',
    d: 'Cocinera corporativa y pretenciosa que busca conquistar la Riviera Nayarit usando atajos químicos y maquinaria importada. Recurre al sabotaje constante para aplastar el talento local.'
  }, {
    id: 'polifemo',
    name: 'Polifemo',
    role: 'El Estibador del Mercado',
    tone: 'neutral',
    tag: 'Guardián',
    img: '../../assets/characters/polifemo-sheet.png',
    d: 'Gigantesco y malhumorado comerciante que vigila la cima de los huacales. Parece intratable, pero su debilidad por las distracciones lo hace vulnerable al ingenio.'
  }],
  receta: [{
    i: 'nutrition',
    n: 'Plátano macho real',
    w: 'Nivel 1 · cima del mercado'
  }, {
    i: 'grain',
    n: 'Harina de trigo',
    w: 'Nivel 1'
  }, {
    i: 'egg',
    n: 'Huevos',
    w: 'Nivel 1'
  }, {
    i: 'cookie',
    n: 'Azúcar pura de caña',
    w: 'Nivel 1'
  }, {
    i: 'spa',
    n: 'Canela en rama',
    w: 'Morral de especias'
  }, {
    i: 'forest',
    n: 'Leña de horno de barro',
    w: 'Nivel 2'
  }]
};
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/companion/data.jsx", error: String((e && e.message) || e) }); }

// ui_kits/juego/Level.jsx
try { (() => {
(() => {
  const {
    Button,
    IconButton,
    HeartMeter,
    HudChip,
    ProgressBar,
    ItemSlot,
    GamePanel,
    Tag
  } = window.LaOdiseaDelPanDePlTanoDesignSystem_cce664;
  const LV = {
    1: {
      title: 'El Mercado Vertical',
      bg: '../../assets/illustrations/nivel1-mercado-vertical.jpg',
      pos: 'center 70%',
      bar: ['Altura', 'height', 'banana'],
      items: [['grain', 'Harina'], ['egg', 'Huevos'], ['cookie', 'Azúcar'], ['nutrition', 'Plátano']]
    },
    2: {
      title: 'El Asedio a la Panadería',
      bg: '../../assets/illustrations/nivel2-patio-horno.jpg',
      pos: 'center',
      bar: ['Cocción del pan', 'local_fire_department', 'cook'],
      items: [['forest', 'Leña'], ['soup_kitchen', 'Olla'], ['fence', 'Barricada'], ['air', 'Ventilador']]
    },
    3: {
      title: 'Carrera por La Tovara',
      bg: '../../assets/illustrations/mapa-ruta-del-pan.png',
      pos: 'center',
      zoom: 'scale(2.2)',
      origin: '80% 85%',
      bar: ['Distancia al muelle', 'directions_run', 'mangrove'],
      items: [['eco', 'Escudo'], ['coffee', 'Café de olla']]
    }
  };
  function Level({
    level,
    onExit,
    onNext
  }) {
    const L = LV[level];
    const [lives, setLives] = React.useState(3);
    const [prog, setProg] = React.useState(level === 2 ? 64 : 38);
    const [got, setGot] = React.useState(level === 1 ? 2 : 1);
    const [state, setState] = React.useState('play');
    const [score, setScore] = React.useState(8200);
    React.useEffect(() => {
      const k = e => {
        if (e.key === 'p' || e.key === 'Escape') setState(s => s === 'play' ? 'pause' : s === 'pause' ? 'play' : s);
      };
      addEventListener('keydown', k);
      return () => removeEventListener('keydown', k);
    }, []);
    const hit = () => {
      const n = lives - 1;
      setLives(n);
      if (n <= 0) setState('over');
    };
    const advance = () => {
      const p = Math.min(100, prog + 22);
      setProg(p);
      setScore(s => s + 1250);
      setGot(g => Math.min(L.items.length, g + 1));
      if (p >= 100) setState('win');
    };
    const retry = () => {
      setLives(3);
      setProg(10);
      setState('play');
    };
    return /*#__PURE__*/React.createElement("div", {
      style: {
        position: 'absolute',
        inset: 0,
        background: 'var(--cafe-900)',
        color: 'var(--crema-50)'
      }
    }, /*#__PURE__*/React.createElement("img", {
      src: L.bg,
      style: {
        position: 'absolute',
        inset: 0,
        width: '100%',
        height: '100%',
        objectFit: 'cover',
        objectPosition: L.pos,
        transform: L.zoom || 'none',
        transformOrigin: L.origin || 'center',
        filter: state === 'play' ? 'none' : 'blur(6px)'
      }
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        position: 'absolute',
        top: 40,
        left: 48,
        right: 48,
        display: 'flex',
        alignItems: 'center',
        gap: 20
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        background: 'var(--cafe-900)',
        border: '3px solid var(--cafe-950)',
        borderRadius: 999,
        padding: '6px 16px',
        boxShadow: 'var(--shadow-pop)'
      }
    }, /*#__PURE__*/React.createElement(HeartMeter, {
      lives: lives,
      size: 40
    })), /*#__PURE__*/React.createElement(HudChip, {
      icon: "star",
      label: "Puntos",
      value: score.toLocaleString('en'),
      accent: true
    }), /*#__PURE__*/React.createElement(HudChip, {
      icon: "timer",
      label: "Tiempo",
      value: "02:47"
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        flex: 1,
        maxWidth: 620,
        marginLeft: 20,
        background: 'var(--cafe-900)',
        border: '3px solid var(--cafe-950)',
        borderRadius: 'var(--radius-lg)',
        padding: '10px 18px',
        boxShadow: 'var(--shadow-pop)'
      }
    }, /*#__PURE__*/React.createElement(ProgressBar, {
      label: L.bar[0],
      icon: L.bar[1],
      tone: L.bar[2],
      value: prog
    })), /*#__PURE__*/React.createElement("div", {
      style: {
        marginLeft: 'auto'
      }
    }, /*#__PURE__*/React.createElement(IconButton, {
      icon: "pause",
      label: "Pausa (P)",
      variant: "dark",
      size: 68,
      onClick: () => setState('pause')
    }))), /*#__PURE__*/React.createElement("div", {
      style: {
        position: 'absolute',
        left: 48,
        bottom: 40,
        display: 'flex',
        alignItems: 'flex-end',
        gap: 20,
        background: 'rgba(28,19,14,.78)',
        border: '3px solid var(--cafe-950)',
        borderRadius: 'var(--radius-xl)',
        padding: '18px 24px 14px'
      }
    }, L.items.map(([ic, lb], i) => /*#__PURE__*/React.createElement(ItemSlot, {
      key: lb,
      icon: ic,
      label: lb,
      collected: i < got,
      active: i === got,
      size: 76
    }))), /*#__PURE__*/React.createElement("div", {
      style: {
        position: 'absolute',
        right: 48,
        bottom: 40,
        display: 'flex',
        gap: 14,
        alignItems: 'center'
      }
    }, /*#__PURE__*/React.createElement(Tag, {
      tone: "dark"
    }, "Demo"), /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      icon: "heart_broken",
      onClick: hit
    }, "Recibir golpe"), /*#__PURE__*/React.createElement(Button, {
      icon: "fast_forward",
      onClick: advance
    }, "Avanzar")), state !== 'play' && /*#__PURE__*/React.createElement("div", {
      style: {
        position: 'absolute',
        inset: 0,
        background: 'var(--scrim)',
        display: 'grid',
        placeItems: 'center'
      }
    }, state === 'pause' && /*#__PURE__*/React.createElement(GamePanel, {
      width: 560,
      eyebrow: `Nivel ${level} · ${L.title}`,
      title: "Pausa",
      actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
        size: "lg",
        fullWidth: true,
        icon: "play_arrow",
        onClick: () => setState('play')
      }, "Continuar"), /*#__PURE__*/React.createElement(Button, {
        size: "lg",
        fullWidth: true,
        variant: "secondary",
        icon: "settings"
      }, "Opciones"), /*#__PURE__*/React.createElement(Button, {
        size: "lg",
        fullWidth: true,
        variant: "ghost",
        icon: "map",
        onClick: onExit
      }, "Salir al mapa"))
    }), state === 'over' && /*#__PURE__*/React.createElement(GamePanel, {
      width: 600,
      tone: "dark",
      eyebrow: "Game Over",
      title: "\xA1Se te quem\xF3 el pan!",
      actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
        size: "lg",
        fullWidth: true,
        icon: "replay",
        onClick: retry
      }, "Reintentar"), /*#__PURE__*/React.createElement(Button, {
        size: "lg",
        fullWidth: true,
        variant: "secondary",
        icon: "map",
        onClick: onExit
      }, "Volver al mapa"))
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        justifyContent: 'center'
      }
    }, /*#__PURE__*/React.createElement(HeartMeter, {
      lives: 0,
      size: 56
    })), /*#__PURE__*/React.createElement("p", {
      style: {
        margin: 0,
        textAlign: 'center',
        font: '400 20px/1.5 var(--font-body)',
        color: 'var(--cafe-300)'
      }
    }, "Madame Circe se r\xEDe desde las vigas. Don Lupe todav\xEDa conf\xEDa en ti.")), state === 'win' && /*#__PURE__*/React.createElement(GamePanel, {
      width: 640,
      eyebrow: level === 3 ? 'Festival Gastronómico de la Riviera Nayarit' : `Nivel ${level} completado`,
      title: level === 3 ? '¡Empleado del Mes!' : '¡Nivel superado!',
      actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
        size: "lg",
        fullWidth: true,
        iconRight: "arrow_forward",
        onClick: onNext
      }, level === 3 ? 'Ver final' : 'Siguiente nivel'), /*#__PURE__*/React.createElement(Button, {
        size: "lg",
        fullWidth: true,
        variant: "secondary",
        icon: "map",
        onClick: onExit
      }, "Mapa"))
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        justifyContent: 'center',
        gap: 16,
        alignItems: 'center'
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        width: 120,
        height: 120,
        borderRadius: 'var(--radius-xl)',
        background: 'var(--cafe-900)',
        display: 'grid',
        placeItems: 'center',
        font: '900 84px/1 var(--font-display)',
        color: 'var(--platano-500)',
        boxShadow: 'var(--shadow-pop)'
      }
    }, "S"), /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        flexDirection: 'column',
        gap: 6
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        font: '700 14px var(--font-body)',
        letterSpacing: '.08em',
        textTransform: 'uppercase',
        color: 'var(--text-muted)'
      }
    }, "Puntuaci\xF3n"), /*#__PURE__*/React.createElement("span", {
      style: {
        font: '800 48px/1 var(--font-display)',
        color: 'var(--text-strong)'
      }
    }, score.toLocaleString('en')))))));
  }
  window.Level = Level;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/juego/Level.jsx", error: String((e && e.message) || e) }); }

// ui_kits/juego/Menu.jsx
try { (() => {
(() => {
  const {
    Button,
    IconButton
  } = window.LaOdiseaDelPanDePlTanoDesignSystem_cce664;
  function MainMenu({
    onPlay
  }) {
    const arc = (size, color, bottom) => /*#__PURE__*/React.createElement("div", {
      style: {
        position: 'absolute',
        left: '50%',
        bottom,
        width: size,
        height: size,
        marginLeft: -size / 2,
        borderRadius: '50%',
        background: color
      }
    });
    return /*#__PURE__*/React.createElement("div", {
      style: {
        position: 'absolute',
        inset: 0,
        background: 'var(--crema-50)',
        overflow: 'hidden'
      }
    }, arc(3400, 'var(--beige-100)', -2280), arc(3000, 'var(--beige-200)', -1980), arc(2600, 'var(--cafe-900)', -1670), /*#__PURE__*/React.createElement("div", {
      style: {
        position: 'absolute',
        left: 0,
        right: 0,
        top: 0,
        bottom: 0,
        justifyContent: 'center',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        gap: 16
      }
    }, /*#__PURE__*/React.createElement("img", {
      src: "../../assets/logo.svg",
      style: {
        width: 200
      }
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        font: '800 88px/0.98 var(--font-display)',
        color: 'var(--platano-500)',
        textAlign: 'center',
        letterSpacing: '-.015em'
      }
    }, "La Odisea del", /*#__PURE__*/React.createElement("br", null), "Pan de Pl\xE1tano"), /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        flexDirection: 'column',
        gap: 18,
        width: 420,
        marginTop: 24
      }
    }, /*#__PURE__*/React.createElement(Button, {
      size: "lg",
      icon: "play_arrow",
      fullWidth: true,
      onClick: onPlay
    }, "Jugar"), /*#__PURE__*/React.createElement(Button, {
      size: "lg",
      variant: "secondary",
      icon: "settings",
      fullWidth: true
    }, "Opciones"), /*#__PURE__*/React.createElement(Button, {
      size: "lg",
      variant: "dark",
      icon: "logout",
      fullWidth: true,
      style: {
        border: '2px solid var(--platano-500)'
      }
    }, "Salir"))), /*#__PURE__*/React.createElement("div", {
      style: {
        position: 'absolute',
        top: 48,
        right: 56,
        display: 'flex',
        gap: 16
      }
    }, /*#__PURE__*/React.createElement(IconButton, {
      icon: "volume_up",
      label: "Audio",
      size: 64
    }), /*#__PURE__*/React.createElement(IconButton, {
      icon: "info",
      label: "Cr\xE9ditos",
      size: 64
    })), /*#__PURE__*/React.createElement("img", {
      src: "../../assets/esrb-e10.png",
      style: {
        position: 'absolute',
        left: 56,
        bottom: 48,
        height: 110
      }
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        position: 'absolute',
        right: 56,
        bottom: 56,
        font: '500 20px var(--font-body)',
        color: 'var(--cafe-300)'
      }
    }, "v0.1 \xB7 San Blas, Nayarit"));
  }
  window.MainMenu = MainMenu;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/juego/Menu.jsx", error: String((e && e.message) || e) }); }

// ui_kits/juego/WorldMap.jsx
try { (() => {
(() => {
  const {
    Button,
    BackButton,
    Tag,
    Icon,
    HudChip
  } = window.LaOdiseaDelPanDePlTanoDesignSystem_cce664;
  const LEVELS = [{
    n: 1,
    name: 'El Mercado Vertical',
    place: 'Mercado Municipal de San Blas',
    genre: 'Plataformas vertical',
    x: 36,
    y: 63,
    icon: 'shopping_basket',
    desc: 'Escala los huacales, esquiva cáscaras y sacos de cebolla y arranca los plátanos machos reales a Polifemo.',
    items: ['Harina', 'Huevos', 'Azúcar', 'Plátano macho']
  }, {
    n: 2,
    name: 'El Asedio a la Panadería',
    place: 'Panadería de piedra',
    genre: 'Tower defense',
    x: 58,
    y: 36,
    icon: 'local_fire_department',
    desc: 'Defiende el horno mientras la barra de cocción avanza: ratas, jejenes, cocodrilos y los ayudantes de Circe.',
    items: ['Leña extra', 'Utensilios-trampa']
  }, {
    n: 3,
    name: 'Carrera por La Tovara',
    place: 'Manglares de La Tovara',
    genre: 'Runner',
    x: 74,
    y: 80,
    icon: 'directions_run',
    desc: 'Corre con la canasta a la espalda por pasarelas y raíces hasta el festival. ¡Que no te arrebaten el pan!',
    items: ['Hoja de plátano (escudo)', 'Café de olla (velocidad)']
  }];
  function WorldMap({
    onBack,
    onPlay
  }) {
    const [sel, setSel] = React.useState(1);
    const L = LEVELS[sel - 1];
    return /*#__PURE__*/React.createElement("div", {
      style: {
        position: 'absolute',
        inset: 0,
        background: 'var(--cafe-900)'
      }
    }, /*#__PURE__*/React.createElement("img", {
      src: "../../assets/illustrations/mapa-ruta-del-pan.png",
      style: {
        position: 'absolute',
        left: 0,
        top: 0,
        width: 1920,
        height: 1080,
        objectFit: 'cover'
      }
    }), LEVELS.map(l => /*#__PURE__*/React.createElement("button", {
      key: l.n,
      onClick: () => setSel(l.n),
      style: {
        position: 'absolute',
        left: `${l.x}%`,
        top: `${l.y}%`,
        transform: `translate(-50%,-50%) scale(${sel === l.n ? 1.12 : 1})`,
        transition: 'transform var(--dur-base) var(--ease-bounce)',
        width: 96,
        height: 96,
        borderRadius: '50%',
        border: '4px solid var(--cafe-950)',
        background: sel === l.n ? 'var(--platano-500)' : 'var(--crema-50)',
        boxShadow: sel === l.n ? '0 0 0 8px rgba(245,183,49,.45), var(--shadow-pop-lg)' : 'var(--shadow-pop-lg)',
        display: 'grid',
        placeItems: 'center',
        cursor: 'pointer',
        font: '800 44px/1 var(--font-display)',
        color: 'var(--cafe-900)'
      }
    }, l.n)), /*#__PURE__*/React.createElement("div", {
      style: {
        position: 'absolute',
        top: 40,
        left: 48,
        right: 48,
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center'
      }
    }, /*#__PURE__*/React.createElement(BackButton, {
      onDark: true,
      onClick: onBack,
      style: {
        height: 56,
        fontSize: 20,
        padding: '0 22px 0 14px'
      }
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        gap: 16
      }
    }, /*#__PURE__*/React.createElement(HudChip, {
      icon: "star",
      label: "Mejor puntaje",
      value: "12,450",
      accent: true
    }), /*#__PURE__*/React.createElement(HudChip, {
      icon: "emoji_events",
      label: "Rango",
      value: "A"
    }))), /*#__PURE__*/React.createElement("div", {
      style: {
        position: 'absolute',
        left: 48,
        bottom: 48,
        width: 520,
        background: 'var(--crema-50)',
        border: '3px solid var(--cafe-950)',
        borderRadius: 'var(--radius-xl)',
        boxShadow: 'var(--shadow-pop-lg), var(--shadow-float)',
        padding: 32,
        display: 'flex',
        flexDirection: 'column',
        gap: 16
      }
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        gap: 10
      }
    }, /*#__PURE__*/React.createElement(Tag, {
      tone: "dark"
    }, "Nivel ", L.n), /*#__PURE__*/React.createElement(Tag, {
      tone: "ally"
    }, L.genre)), /*#__PURE__*/React.createElement("div", {
      style: {
        font: '800 44px/1.05 var(--font-display)',
        color: 'var(--text-strong)'
      }
    }, L.name), /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        alignItems: 'center',
        gap: 8,
        font: '500 18px var(--font-body)',
        color: 'var(--text-muted)'
      }
    }, /*#__PURE__*/React.createElement(Icon, {
      name: "location_on",
      size: 22,
      color: "var(--manglar-600)"
    }), L.place), /*#__PURE__*/React.createElement("p", {
      style: {
        margin: 0,
        font: '400 19px/1.5 var(--font-body)',
        color: 'var(--text-body)',
        textWrap: 'pretty'
      }
    }, L.desc), /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        gap: 8,
        flexWrap: 'wrap'
      }
    }, L.items.map(i => /*#__PURE__*/React.createElement(Tag, {
      key: i,
      tone: "ingredient"
    }, i))), /*#__PURE__*/React.createElement(Button, {
      size: "lg",
      icon: "play_arrow",
      fullWidth: true,
      onClick: () => onPlay(L.n),
      style: {
        marginTop: 8
      }
    }, "Jugar nivel ", L.n)));
  }
  window.WorldMap = WorldMap;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/juego/WorldMap.jsx", error: String((e && e.message) || e) }); }

__ds_ns.ActionCard = __ds_scope.ActionCard;

__ds_ns.CharacterCard = __ds_scope.CharacterCard;

__ds_ns.BackButton = __ds_scope.BackButton;

__ds_ns.Badge = __ds_scope.Badge;

__ds_ns.Button = __ds_scope.Button;

__ds_ns.Icon = __ds_scope.Icon;

__ds_ns.IconButton = __ds_scope.IconButton;

__ds_ns.Tag = __ds_scope.Tag;

__ds_ns.GamePanel = __ds_scope.GamePanel;

__ds_ns.HeartMeter = __ds_scope.HeartMeter;

__ds_ns.HudChip = __ds_scope.HudChip;

__ds_ns.ItemSlot = __ds_scope.ItemSlot;

__ds_ns.ProgressBar = __ds_scope.ProgressBar;

__ds_ns.TopBar = __ds_scope.TopBar;

})();
