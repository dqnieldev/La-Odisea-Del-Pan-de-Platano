export interface IconButtonProps {
  icon: string;
  /** Accessible label (aria-label + tooltip) */
  label: string;
  size?: number;
  variant?: 'light' | 'accent' | 'dark';
  disabled?: boolean;
  onClick?: () => void;
  style?: React.CSSProperties;
}
export declare function IconButton(props: IconButtonProps): JSX.Element;
