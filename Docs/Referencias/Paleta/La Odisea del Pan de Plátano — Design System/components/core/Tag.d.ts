export interface TagProps {
  children?: React.ReactNode;
  /** ally/ingredient = Verde Manglar; rival = chile; accent = plátano (p.ej. etiqueta "Wiki") */
  tone?: 'ally' | 'ingredient' | 'rival' | 'neutral' | 'accent' | 'dark';
  icon?: string;
  style?: React.CSSProperties;
}
export declare function Tag(props: TagProps): JSX.Element;
