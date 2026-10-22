export interface CharacterCardProps {
  name: string;
  /** p.ej. "El Panadero" */
  role?: string;
  image?: string;
  tone?: 'ally' | 'rival' | 'neutral' | 'accent';
  tagLabel?: string;
  description?: string;
  compact?: boolean;
  onClick?: () => void;
  style?: React.CSSProperties;
}
export declare function CharacterCard(props: CharacterCardProps): JSX.Element;
