/**
 * Panel modal del juego: Pausa, Game Over, Victoria, Nivel completado.
 * @startingPoint section="Feedback" subtitle="Panel de Pausa / Game Over / Victoria" viewport="700x420"
 */
export interface GamePanelProps {
  title: string;
  eyebrow?: string;
  tone?: 'light' | 'dark';
  children?: React.ReactNode;
  /** Botones apilados al pie */
  actions?: React.ReactNode;
  width?: number;
  style?: React.CSSProperties;
}
export declare function GamePanel(props: GamePanelProps): JSX.Element;
