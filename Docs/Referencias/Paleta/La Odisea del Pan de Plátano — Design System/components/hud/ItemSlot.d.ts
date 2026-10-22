export interface ItemSlotProps {
  /** Material Symbols name (use when no sprite yet) */
  icon?: string;
  /** Sprite/PNG del ítem */
  image?: string;
  label?: string;
  count?: number;
  /** false = aún no recolectado (apagado) */
  collected?: boolean;
  active?: boolean;
  size?: number;
  style?: React.CSSProperties;
}
export declare function ItemSlot(props: ItemSlotProps): JSX.Element;
