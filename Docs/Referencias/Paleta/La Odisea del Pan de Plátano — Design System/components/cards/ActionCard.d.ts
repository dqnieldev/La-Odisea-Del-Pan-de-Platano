/**
 * Tarjeta de acción del menú principal (Ver Historia, Personajes, Receta) con flecha amarilla.
 * @startingPoint section="Cards" subtitle="Tarjetas de acción del menú principal" viewport="700x380"
 */
export interface ActionCardProps {
  title: string;
  subtitle?: string;
  /** Material Symbols name shown in the café tile */
  icon?: string;
  /** Inactive / bloqueado state */
  locked?: boolean;
  onClick?: () => void;
  style?: React.CSSProperties;
}
export declare function ActionCard(props: ActionCardProps): JSX.Element;
