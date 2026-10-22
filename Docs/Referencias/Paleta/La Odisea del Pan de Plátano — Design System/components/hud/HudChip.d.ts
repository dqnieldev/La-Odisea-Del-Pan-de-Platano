export interface HudChipProps {
  icon?: string;
  value: React.ReactNode;
  /** Etiqueta pequeña en mayúsculas (Puntos, Tiempo) */
  label?: string;
  accent?: boolean;
  style?: React.CSSProperties;
}
export declare function HudChip(props: HudChipProps): JSX.Element;
