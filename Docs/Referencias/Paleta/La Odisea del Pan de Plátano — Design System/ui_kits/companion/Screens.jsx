(() => {
const { TopBar, ActionCard, CharacterCard, Tag, Icon } = window.LaOdiseaDelPanDePlTanoDesignSystem_cce664;
const D = window.ODISEA;
const Body = ({ children, pad = 16 }) => <div style={{ flex: 1, overflowY: 'auto', padding: `${pad}px ${pad}px 40px`, display: 'flex', flexDirection: 'column', gap: 14 }}>{children}</div>;
function HomeScreen({ go }) {
  return <>
    <TopBar title="La Odisea del Pan de Plátano" badge="Wiki" rightIcon="info" />
    <Body>
      <div style={{ borderRadius: 'var(--radius-lg)', overflow: 'hidden', border: '2px solid var(--cafe-900)', boxShadow: 'var(--shadow-pop)', position: 'relative', height: 170, flex: 'none' }}>
        <img src="../../assets/illustrations/mapa-ruta-del-pan.png" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
        <div style={{ position: 'absolute', left: 0, right: 0, bottom: 0, padding: '28px 14px 12px', background: 'linear-gradient(transparent, rgba(28,19,14,.85))', font: '700 18px/1.2 var(--font-display)', color: 'var(--crema-50)' }}>La Ruta del Pan · San Blas, Nayarit</div>
      </div>
      <ActionCard icon="auto_stories" title="Ver Historia" subtitle="Prólogo y tres actos" onClick={() => go('historia')} />
      <ActionCard icon="groups" title="Personajes" subtitle="Héroes, aliados y rivales" onClick={() => go('personajes')} />
      <ActionCard icon="bakery_dining" title="Receta" subtitle="Ingredientes del pan definitivo" onClick={() => go('receta')} />
      <ActionCard icon="sports_esports" title="Niveles" subtitle="Disponible al terminar el Acto I" locked />
    </Body>
  </>;
}
function HistoriaScreen({ back }) {
  const [open, setOpen] = React.useState(0);
  return <>
    <TopBar title="Historia" onBack={back} />
    <Body>
      <img src="../../assets/illustrations/storyboard.png" style={{ width: '100%', borderRadius: 'var(--radius-lg)', border: '2px solid var(--cafe-900)', flex: 'none' }} />
      {D.actos.map((a, i) => <div key={a.k} onClick={() => setOpen(open === i ? -1 : i)} style={{ background: 'var(--surface-card)', border: '2px solid var(--cafe-900)', borderRadius: 'var(--radius-lg)', padding: 14, boxShadow: 'var(--shadow-pop-sm)', cursor: 'pointer', display: 'flex', flexDirection: 'column', gap: 8 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}><Tag tone={i === 0 ? 'accent' : 'dark'}>{a.k}</Tag><span style={{ flex: 1, font: '700 16px/1.2 var(--font-display)', color: 'var(--text-strong)' }}>{a.t}</span><Icon name={open === i ? 'expand_less' : 'expand_more'} /></div>
        {open === i && <p style={{ margin: 0, font: '400 15px/1.5 var(--font-body)', color: 'var(--text-body)' }}>{a.s}</p>}
      </div>)}
    </Body>
  </>;
}
function PersonajesScreen({ back, go }) {
  return <>
    <TopBar title="Personajes" onBack={back} />
    <Body><div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
      {D.personajes.map(c => <CharacterCard key={c.id} compact name={c.name} role={c.role} tone={c.tone} image={c.img} onClick={() => go('personaje', c)} />)}
    </div></Body>
  </>;
}
function PersonajeScreen({ back, p }) {
  return <>
    <TopBar title={p.name} onBack={back} />
    <Body><CharacterCard name={p.name} role={p.role} tone={p.tone} tagLabel={p.tag} image={p.img} description={p.d} /></Body>
  </>;
}
function RecetaScreen({ back }) {
  const [done, setDone] = React.useState({ 0: true, 1: true });
  return <>
    <TopBar title="Receta" onBack={back} />
    <Body>
      <div style={{ font: '800 26px/1.1 var(--font-display)', color: 'var(--text-strong)' }}>El pan de plátano definitivo</div>
      <p style={{ margin: 0, font: '400 15px/1.5 var(--font-body)', color: 'var(--text-muted)' }}>Sin atajos de supermercado: los frutos más selectos de la costa.</p>
      {D.receta.map((r, i) => <div key={r.n} onClick={() => setDone({ ...done, [i]: !done[i] })} style={{ display: 'flex', alignItems: 'center', gap: 12, background: 'var(--surface-card)', border: '2px solid var(--cafe-900)', borderRadius: 'var(--radius-md)', padding: 10, cursor: 'pointer' }}>
        <span style={{ width: 44, height: 44, borderRadius: 'var(--radius-sm)', background: done[i] ? 'var(--manglar-600)' : 'var(--beige-200)', color: done[i] ? 'var(--crema-50)' : 'var(--cafe-700)', display: 'grid', placeItems: 'center' }}><Icon name={r.i} /></span>
        <span style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: 3 }}><span style={{ font: '600 16px var(--font-body)', color: 'var(--text-strong)' }}>{r.n}</span><span style={{ font: '400 13px var(--font-body)', color: 'var(--text-muted)' }}>{r.w}</span></span>
        {done[i] ? <Tag tone="ally" icon="check">Tengo</Tag> : <Tag>Falta</Tag>}
      </div>)}
    </Body>
  </>;
}
Object.assign(window, { HomeScreen, HistoriaScreen, PersonajesScreen, PersonajeScreen, RecetaScreen });
})();
