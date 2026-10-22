(() => {
const { Button, IconButton, HeartMeter, HudChip, ProgressBar, ItemSlot, GamePanel, Tag } = window.LaOdiseaDelPanDePlTanoDesignSystem_cce664;
const LV = {
  1: { title: 'El Mercado Vertical', bg: '../../assets/illustrations/nivel1-mercado-vertical.jpg', pos: 'center 70%', bar: ['Altura', 'height', 'banana'], items: [['grain', 'Harina'], ['egg', 'Huevos'], ['cookie', 'Azúcar'], ['nutrition', 'Plátano']] },
  2: { title: 'El Asedio a la Panadería', bg: '../../assets/illustrations/nivel2-patio-horno.jpg', pos: 'center', bar: ['Cocción del pan', 'local_fire_department', 'cook'], items: [['forest', 'Leña'], ['soup_kitchen', 'Olla'], ['fence', 'Barricada'], ['air', 'Ventilador']] },
  3: { title: 'Carrera por La Tovara', bg: '../../assets/illustrations/mapa-ruta-del-pan.png', pos: 'center', zoom: 'scale(2.2)', origin: '80% 85%', bar: ['Distancia al muelle', 'directions_run', 'mangrove'], items: [['eco', 'Escudo'], ['coffee', 'Café de olla']] },
};
function Level({ level, onExit, onNext }) {
  const L = LV[level];
  const [lives, setLives] = React.useState(3);
  const [prog, setProg] = React.useState(level === 2 ? 64 : 38);
  const [got, setGot] = React.useState(level === 1 ? 2 : 1);
  const [state, setState] = React.useState('play');
  const [score, setScore] = React.useState(8200);
  React.useEffect(() => { const k = e => { if (e.key === 'p' || e.key === 'Escape') setState(s => s === 'play' ? 'pause' : s === 'pause' ? 'play' : s); }; addEventListener('keydown', k); return () => removeEventListener('keydown', k); }, []);
  const hit = () => { const n = lives - 1; setLives(n); if (n <= 0) setState('over'); };
  const advance = () => { const p = Math.min(100, prog + 22); setProg(p); setScore(s => s + 1250); setGot(g => Math.min(L.items.length, g + 1)); if (p >= 100) setState('win'); };
  const retry = () => { setLives(3); setProg(10); setState('play'); };
  return (
    <div style={{ position: 'absolute', inset: 0, background: 'var(--cafe-900)', color: 'var(--crema-50)' }}>
      <img src={L.bg} style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', objectFit: 'cover', objectPosition: L.pos, transform: L.zoom || 'none', transformOrigin: L.origin || 'center', filter: state === 'play' ? 'none' : 'blur(6px)' }} />
      <div style={{ position: 'absolute', top: 40, left: 48, right: 48, display: 'flex', alignItems: 'center', gap: 20 }}>
        <div style={{ background: 'var(--cafe-900)', border: '3px solid var(--cafe-950)', borderRadius: 999, padding: '6px 16px', boxShadow: 'var(--shadow-pop)' }}><HeartMeter lives={lives} size={40} /></div>
        <HudChip icon="star" label="Puntos" value={score.toLocaleString('en')} accent />
        <HudChip icon="timer" label="Tiempo" value="02:47" />
        <div style={{ flex: 1, maxWidth: 620, marginLeft: 20, background: 'var(--cafe-900)', border: '3px solid var(--cafe-950)', borderRadius: 'var(--radius-lg)', padding: '10px 18px', boxShadow: 'var(--shadow-pop)' }}><ProgressBar label={L.bar[0]} icon={L.bar[1]} tone={L.bar[2]} value={prog} /></div>
        <div style={{ marginLeft: 'auto' }}><IconButton icon="pause" label="Pausa (P)" variant="dark" size={68} onClick={() => setState('pause')} /></div>
      </div>
      <div style={{ position: 'absolute', left: 48, bottom: 40, display: 'flex', alignItems: 'flex-end', gap: 20, background: 'rgba(28,19,14,.78)', border: '3px solid var(--cafe-950)', borderRadius: 'var(--radius-xl)', padding: '18px 24px 14px' }}>
        {L.items.map(([ic, lb], i) => <ItemSlot key={lb} icon={ic} label={lb} collected={i < got} active={i === got} size={76} />)}
      </div>
      <div style={{ position: 'absolute', right: 48, bottom: 40, display: 'flex', gap: 14, alignItems: 'center' }}>
        <Tag tone="dark">Demo</Tag>
        <Button variant="secondary" icon="heart_broken" onClick={hit}>Recibir golpe</Button>
        <Button icon="fast_forward" onClick={advance}>Avanzar</Button>
      </div>
      {state !== 'play' && <div style={{ position: 'absolute', inset: 0, background: 'var(--scrim)', display: 'grid', placeItems: 'center' }}>
        {state === 'pause' && <GamePanel width={560} eyebrow={`Nivel ${level} · ${L.title}`} title="Pausa" actions={<><Button size="lg" fullWidth icon="play_arrow" onClick={() => setState('play')}>Continuar</Button><Button size="lg" fullWidth variant="secondary" icon="settings">Opciones</Button><Button size="lg" fullWidth variant="ghost" icon="map" onClick={onExit}>Salir al mapa</Button></>} />}
        {state === 'over' && <GamePanel width={600} tone="dark" eyebrow="Game Over" title="¡Se te quemó el pan!" actions={<><Button size="lg" fullWidth icon="replay" onClick={retry}>Reintentar</Button><Button size="lg" fullWidth variant="secondary" icon="map" onClick={onExit}>Volver al mapa</Button></>}><div style={{ display: 'flex', justifyContent: 'center' }}><HeartMeter lives={0} size={56} /></div><p style={{ margin: 0, textAlign: 'center', font: '400 20px/1.5 var(--font-body)', color: 'var(--cafe-300)' }}>Madame Circe se ríe desde las vigas. Don Lupe todavía confía en ti.</p></GamePanel>}
        {state === 'win' && <GamePanel width={640} eyebrow={level === 3 ? 'Festival Gastronómico de la Riviera Nayarit' : `Nivel ${level} completado`} title={level === 3 ? '¡Empleado del Mes!' : '¡Nivel superado!'} actions={<><Button size="lg" fullWidth iconRight="arrow_forward" onClick={onNext}>{level === 3 ? 'Ver final' : 'Siguiente nivel'}</Button><Button size="lg" fullWidth variant="secondary" icon="map" onClick={onExit}>Mapa</Button></>}>
          <div style={{ display: 'flex', justifyContent: 'center', gap: 16, alignItems: 'center' }}><div style={{ width: 120, height: 120, borderRadius: 'var(--radius-xl)', background: 'var(--cafe-900)', display: 'grid', placeItems: 'center', font: '900 84px/1 var(--font-display)', color: 'var(--platano-500)', boxShadow: 'var(--shadow-pop)' }}>S</div><div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}><span style={{ font: '700 14px var(--font-body)', letterSpacing: '.08em', textTransform: 'uppercase', color: 'var(--text-muted)' }}>Puntuación</span><span style={{ font: '800 48px/1 var(--font-display)', color: 'var(--text-strong)' }}>{score.toLocaleString('en')}</span></div></div>
        </GamePanel>}
      </div>}
    </div>
  );
}
window.Level = Level;
})();
