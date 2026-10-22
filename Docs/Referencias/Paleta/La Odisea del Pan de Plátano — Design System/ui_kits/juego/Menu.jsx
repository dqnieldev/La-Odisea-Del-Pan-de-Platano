(() => {
const { Button, IconButton } = window.LaOdiseaDelPanDePlTanoDesignSystem_cce664;
function MainMenu({ onPlay }) {
  const arc = (size, color, bottom) => <div style={{ position: 'absolute', left: '50%', bottom, width: size, height: size, marginLeft: -size / 2, borderRadius: '50%', background: color }}></div>;
  return (
    <div style={{ position: 'absolute', inset: 0, background: 'var(--crema-50)', overflow: 'hidden' }}>
      {arc(3400, 'var(--beige-100)', -2280)}{arc(3000, 'var(--beige-200)', -1980)}{arc(2600, 'var(--cafe-900)', -1670)}
      <div style={{ position: 'absolute', left: 0, right: 0, top: 0, bottom: 0, justifyContent: 'center', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 16 }}>
        <img src="../../assets/logo.svg" style={{ width: 200 }} />
        <div style={{ font: '800 88px/0.98 var(--font-display)', color: 'var(--platano-500)', textAlign: 'center', letterSpacing: '-.015em' }}>La Odisea del<br/>Pan de Plátano</div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 18, width: 420, marginTop: 24 }}>
          <Button size="lg" icon="play_arrow" fullWidth onClick={onPlay}>Jugar</Button>
          <Button size="lg" variant="secondary" icon="settings" fullWidth>Opciones</Button>
          <Button size="lg" variant="dark" icon="logout" fullWidth style={{ border: '2px solid var(--platano-500)' }}>Salir</Button>
        </div>
      </div>
      <div style={{ position: 'absolute', top: 48, right: 56, display: 'flex', gap: 16 }}><IconButton icon="volume_up" label="Audio" size={64} /><IconButton icon="info" label="Créditos" size={64} /></div>
      <img src="../../assets/esrb-e10.png" style={{ position: 'absolute', left: 56, bottom: 48, height: 110 }} />
      <div style={{ position: 'absolute', right: 56, bottom: 56, font: '500 20px var(--font-body)', color: 'var(--cafe-300)' }}>v0.1 · San Blas, Nayarit</div>
    </div>
  );
}
window.MainMenu = MainMenu;
})();
