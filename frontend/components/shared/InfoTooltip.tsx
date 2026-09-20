import { Icon } from "./Icon";

interface InfoTooltipProps {
  text: string;
}

/** Ícone de informação reutilizável: balão explicativo via hover, toque ou foco de teclado. */
export function InfoTooltip({ text }: InfoTooltipProps) {
  return (
    <span className="info-tooltip">
      <button type="button" className="info-tooltip-trigger" aria-label={text}>
        <Icon name="info" size={12} />
      </button>
      <span className="info-tooltip-bubble" role="tooltip">
        {text}
      </span>
    </span>
  );
}
