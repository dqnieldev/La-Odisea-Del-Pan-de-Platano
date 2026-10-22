export interface ProgressBarProps {
  /** 0–100 */
  value?: number;
  label?: string;
  icon?: string;
  /** cook = barra de cocción (terracota) */
  tone?: 'cook' | 'banana' | 'mangrove' | 'danger';
  height?: number;
  showValue?: boolean;
  style?: React.CSSProperties;
}
export declare function ProgressBar(props: ProgressBarProps): JSX.Element;
