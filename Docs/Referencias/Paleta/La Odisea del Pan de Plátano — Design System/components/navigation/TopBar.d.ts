/**
 * Barra superior del companion: ← Volver · título centrado · ⓘ.
 * @startingPoint section="Navigation" subtitle="Barra superior con Volver, título y ⓘ" viewport="700x200"
 */
export interface TopBarProps {
  title: string;
  onBack?: () => void;
  rightIcon?: string | null;
  rightLabel?: string;
  onRight?: () => void;
  /** Etiqueta destacada junto al título (p.ej. "Wiki") */
  badge?: string;
  style?: React.CSSProperties;
}
export declare function TopBar(props: TopBarProps): JSX.Element;
