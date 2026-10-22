export interface IconProps {
  /** Material Symbols Rounded glyph name, e.g. "favorite", "bakery_dining" */
  name: string;
  size?: number;
  /** Filled (default, brand standard) vs outline */
  fill?: boolean;
  weight?: number;
  color?: string;
  style?: React.CSSProperties;
}
export declare function Icon(props: IconProps): JSX.Element;
