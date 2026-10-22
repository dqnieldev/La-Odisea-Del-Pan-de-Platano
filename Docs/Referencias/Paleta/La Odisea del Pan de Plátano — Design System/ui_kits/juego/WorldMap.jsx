(() => {
const { Button, BackButton, Tag, Icon, HudChip } = window.LaOdiseaDelPanDePlTanoDesignSystem_cce664;
const LEVELS = [
  { n: 1, name: 'El Mercado Vertical', place: 'Mercado Municipal de San Blas', genre: 'Plataformas vertical', x: 36, y: 63, icon: 'shopping_basket', desc: 'Escala los huacales, esquiva cáscaras y sacos de cebolla y arranca los plátanos machos reales a Polifemo.', items: ['Harina', 'Huevos', 'Azúcar', 'Plátano macho'] },
  { n: 2, name: 'El Asedio a la Panadería', place: 'Panadería de piedra', genre: 'Tower defense', x: 58, y: 36, icon: 'local_fire_department', desc: 'Defiende el horno mientras la barra de cocción avanza: ratas, jejenes, cocodrilos y los ayudantes de Circe.', items: ['Leña extra', 'Utensilios-trampa'] },
  { n: 3, name: 'Carrera por La Tovara', place: 'Manglares de La Tovara', genre: 'Runner', x: 74, y: 80, icon: 'directions_run', desc: 'Corre con la canasta a la espalda por pasarelas y raíces hasta el festival. ¡Que no te arrebaten el pan!', items: ['Hoja de plátano (escudo)', 'Café de olla (velocidad)'] },
];
function WorldMap({ onBack, onPlay }) {
  const [sel, setSel] = React.useState(1);
  const L = LEVELS[sel - 1];
  return (
    <div style={{ position: 'absolute', inset: 0, background: 'var(--cafe-900)' }}>
      <img src="../../assets/illustrations/mapa-ruta-del-pan.png" style={{ position: 'absolute', left: 0, top: 0, width: 1920, height: 1080, objectFit: 'cover' }} />
      {LEVELS.map(l => (
        <button key={l.n} onClick={() => setSel(l.n)} style={{ position: 'absolute', left: `${l.x}%`, top: `${l.y}%`, transform: `translate(-50%,-50%) scale(${sel === l.n ? 1.12 : 1})`, transition: 'transform var(--dur-base) var(--ease-bounce)', width: 96, height: 96, borderRadius: '50%', border: '4px solid var(--cafe-950)', background: sel === l.n ? 'var(--platano-500)' : 'var(--crema-50)', boxShadow: sel === l.n ? '0 0 0 8px rgba(245,183,49,.45), var(--shadow-pop-lg)' : 'var(--shadow-pop-lg)', display: 'grid', placeItems: 'center', cursor: 'pointer', font: '800 44px/1 var(--font-display)', color: 'var(--cafe-900)' }}>{l.n}</button>
      ))}
      <div style={{ position: 'absolute', top: 40, left: 48, right: 48, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <BackButton onDark onClick={onBack} style={{ height: 56, fontSize: 20, padding: '0 22px 0 14px' }} />
        <div style={{ display: 'flex', gap: 16 }}><HudChip icon="star" label="Mejor puntaje" value="12,450" accent /><HudChip icon="emoji_events" label="Rango" value="A" /></div>
      </div>
      <div style={{ position: 'absolute', left: 48, bottom: 48, width: 520, background: 'var(--crema-50)', border: '3px solid var(--cafe-950)', borderRadius: 'var(--radius-xl)', boxShadow: 'var(--shadow-pop-lg), var(--shadow-float)', padding: 32, display: 'flex', flexDirection: 'column', gap: 16 }}>
        <div style={{ display: 'flex', gap: 10 }}><Tag tone="dark">Nivel {L.n}</Tag><Tag tone="ally">{L.genre}</Tag></div>
        <div style={{ font: '800 44px/1.05 var(--font-display)', color: 'var(--text-strong)' }}>{L.name}</div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, font: '500 18px var(--font-body)', color: 'var(--text-muted)' }}><Icon name="location_on" size={22} color="var(--manglar-600)" />{L.place}</div>
        <p style={{ margin: 0, font: '400 19px/1.5 var(--font-body)', color: 'var(--text-body)', textWrap: 'pretty' }}>{L.desc}</p>
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>{L.items.map(i => <Tag key={i} tone="ingredient">{i}</Tag>)}</div>
        <Button size="lg" icon="play_arrow" fullWidth onClick={() => onPlay(L.n)} style={{ marginTop: 8 }}>Jugar nivel {L.n}</Button>
      </div>
    </div>
  );
}
window.WorldMap = WorldMap;
})();
