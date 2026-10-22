export interface BackButtonProps {
  /** Default "Volver" */
  label?: string;
  onClick?: () => void;
  onDark?: boolean;
  style?: React.CSSProperties;
}
export declare function BackButton(props: BackButtonProps): JSX.Element;
