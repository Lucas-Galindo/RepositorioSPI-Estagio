"use client";

import { useRef, useState, type CSSProperties } from "react";
import { Calendario } from "@/components/shared/Calendario";
import { Icon } from "@/components/shared/Icon";
import { lerMes } from "@/lib/calendario";

/** `yyyy-MM` -> `mm/aaaa` (parsing manual de texto, sem `Date`). */
function mesParaTexto(valor: string): string {
  const m = lerMes(valor);
  if (!m) return "";
  return `${String(m.mes).padStart(2, "0")}/${String(m.ano).padStart(4, "0")}`;
}

function mascarar(digitos: string): string {
  const m = digitos.slice(0, 2);
  const a = digitos.slice(2, 6);
  return a ? `${m}/${a}` : m;
}

/** `mm/aaaa` completo e valido -> `yyyy-MM`; senao `null`. */
function textoParaMes(texto: string): string | null {
  const digitos = texto.replace(/\D/g, "");
  if (digitos.length !== 6) return null;
  const mes = Number(digitos.slice(0, 2));
  if (mes < 1 || mes > 12) return null;
  return `${digitos.slice(2, 6)}-${digitos.slice(0, 2)}`;
}

interface MonthInputProps {
  /** `yyyy-MM` ou vazio. */
  value: string;
  onChange: (value: string) => void;
  required?: boolean;
  id?: string;
  className?: string;
  title?: string;
  disabled?: boolean;
  ariaLabel?: string;
  style?: CSSProperties;
}

/**
 * specs/070: campo de competencia (mes e ano) mascarado `mm/aaaa`, com o botao de calendario dentro do campo
 * (mesmo padrao do `DateInput`). Substitui o campo de mes nativo do navegador. O valor trocado com as telas continua
 * `yyyy-MM` (ou vazio), o mesmo contrato do campo nativo; o seletor de meses e o `Calendario` do sistema.
 */
export function MonthInput({ value, onChange, required, id, className, title, disabled, ariaLabel, style }: MonthInputProps) {
  const [envolucro, setEnvolucro] = useState<HTMLSpanElement | null>(null);
  const botaoRef = useRef<HTMLButtonElement>(null);
  const [aberto, setAberto] = useState(false);
  const [texto, setTexto] = useState(() => mesParaTexto(value));
  const [ultimoValue, setUltimoValue] = useState(value);

  // Ajusta o texto quando o valor muda por fora (padrao "adjusting state when a prop changes").
  if (value !== ultimoValue) {
    setUltimoValue(value);
    setTexto(mesParaTexto(value));
  }

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const digitos = e.target.value.replace(/\D/g, "").slice(0, 6);
    const mascarado = mascarar(digitos);
    setTexto(mascarado);
    if (digitos.length === 0) {
      onChange("");
      return;
    }
    const mes = textoParaMes(mascarado);
    if (mes) onChange(mes);
  }

  function fechar() {
    setAberto(false);
    botaoRef.current?.focus();
  }

  const calendarioAberto = aberto && !disabled;

  return (
    <span ref={setEnvolucro} className="date-wrap" style={style}>
      <input
        type="text"
        inputMode="numeric"
        autoComplete="off"
        placeholder="mm/aaaa"
        value={texto}
        onChange={handleChange}
        required={required}
        disabled={disabled}
        id={id}
        title={title}
        aria-label={ariaLabel}
        className={["date-input", className].filter(Boolean).join(" ")}
        maxLength={7}
      />
      <button
        ref={botaoRef}
        type="button"
        className="date-cal-btn"
        aria-label="Abrir seletor de mês"
        title="Abrir seletor de mês"
        aria-haspopup="dialog"
        aria-expanded={calendarioAberto}
        disabled={disabled}
        onClick={() => setAberto((a) => !a)}
      >
        <Icon name="cal" size={14} />
      </button>
      {calendarioAberto && (
        <Calendario
          modo="mes"
          valor={textoParaMes(texto) ?? ""}
          ancora={envolucro}
          aoEscolher={onChange}
          aoLimpar={() => onChange("")}
          aoFechar={fechar}
        />
      )}
    </span>
  );
}
