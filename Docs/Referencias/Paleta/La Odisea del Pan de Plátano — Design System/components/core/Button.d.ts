/**
 * Botón de menú/CTA con contorno café y sombra sólida.
 * @startingPoint section="Core" subtitle="Botones de menú (Jugar, Opciones, Salir)" viewport="700x300"
 */
export interface ButtonProps {
  children?: React.ReactNode;
  variant?: 'primary' | 'secondary' | 'mangrove' | 'dark' | 'ghost';
  size?: 'sm' | 'md' | 'lg';
  /** Material Symbols name */
  icon?: string;
  iconRight?: string;
  disabled?: boolean;
  fullWidth?: boolean;
  onClick?: () => void;
  style?: React.CSSProperties;
}
export declare function Button(props: ButtonProps): JSX.Element;
