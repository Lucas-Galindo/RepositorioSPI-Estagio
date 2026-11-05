"use client";

import { useRef, useState, type CSSProperties } from "react";
import { Calendario } from "@/components/shared/Calendario";
import { Icon } from "@/components/shared/Icon";

/**
 * Parsing manual de string (nunca `new Date()`), mesma tecnica de `lib/format.ts`:
 * `new Date("yyyy-MM-dd")` e interpretado como UTC meia-noite e pode exibir o dia
 * anterior/seguinte dependendo do fuso do navegador.
 */
function isoParaTexto(iso: string): string {
  if (!iso) return "";
  const [y, m, d] = iso.split("-");
  if (!y || !m || !d) return "";
  return `${d}/${m}/${y}`;
}

function mascarar(digitos: string): string {
  const d = digitos.slice(0, 2);
  const m = digitos.slice(2, 4);
  const y = digitos.slice(4, 8);
  let texto = d;
  if (m) texto += `/${m}`;
  if (y) texto += `/${y}`;
  return texto;
}

function textoParaIso(texto: string): string | null {
  const digitos = texto.replace(/\D/g, "");
  if (digitos.length !== 8) return null;
  const d = digitos.slice(0, 2);
  const m = digitos.slice(2, 4);
  const y = digitos.slice(4, 8);
  const dia = Number(d);
  const mes = Number(m);
  if (dia < 1 || dia > 31 || mes < 1 || mes > 12) return null;
  return `${y}-${m}-${d}`;
}

interface DateInputProps {
  value: string;
  onChange: (value: string) => void;
  required?: boolean;
  id?: string;
  className?: string;
  title?: string;
  disabled?: boolean;
  /** Rotulo de acessibilidade do campo de texto (ex.: linhas de uma tabela de parcelas). */
  ariaLabel?: string;
  /** Estilo do involucro do campo (ex.: `flex` e `minWidth` em linhas de formulario). */
  style?: CSSProperties;
  /** yyyy-MM-dd: o calendario do sistema bloqueia dias anteriores. A digitacao nao e bloqueada (o servidor valida). */
  min?: string;
}

/**
 * Campo de data mascarado dd/mm/aaaa — substitui o campo de data nativo do navegador para que o
 * formato exibido/aceito nao dependa do idioma/SO do navegador da usuaria. O valor
 * emitido/recebido continua `yyyy-MM-dd`, o mesmo contrato do input nativo que substitui.
 *
 * specs/069/070: dentro do campo ha um botao de calendario que abre o calendario proprio do sistema
 * (`Calendario`), desenhado fora do campo para nao ser cortado por modais e tabelas. O dia escolhido volta
 * ja em `yyyy-MM-dd`, entao nao ha conversao de data nem risco de dia deslocado por fuso. O campo de texto
 * continua sendo o que a usuaria ve e digita; o calendario abre no mes da data completa do TEXTO exibido
 * (texto vazio, parcial ou invalido: abre em hoje, sem mexer no que ela digitou).
 */
export function DateInput({ value, onChange, required, id, className, title, disabled, ariaLabel, style, min }: DateInputProps) {
  const [envolucro, setEnvolucro] = useState<HTMLSpanElement | null>(null);
  const botaoRef = useRef<HTMLButtonElement>(null);
  const [aberto, setAberto] = useState(false);
  const [texto, setTexto] = useState(() => isoParaTexto(value));
  const [ultimoValue, setUltimoValue] = useState(value);

  // Ajusta o texto exibido quando o valor muda por fora (ex: reset de formulario),
  // sem useEffect — segue o padrao recomendado pelo React para "adjusting state
  // when a prop changes" (evita o set-state-in-effect / cascading renders).
  if (value !== ultimoValue) {
    setUltimoValue(value);
    setTexto(isoParaTexto(value));
  }

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const digitos = e.target.value.replace(/\D/g, "").slice(0, 8);
    const mascarado = mascarar(digitos);
    setTexto(mascarado);
    if (digitos.length === 0) {
      onChange("");
      return;
    }
    const iso = textoParaIso(mascarado);
    if (iso) onChange(iso);
  }

  function fechar() {
    setAberto(false);
    botaoRef.current?.focus();
  }

  // Campo desabilitado depois de aberto: o calendario nao fica solto.
  const calendarioAberto = aberto && !disabled;

  return (
    <span ref={setEnvolucro} className="date-wrap" style={style}>
      <input
        type="text"
        inputMode="numeric"
        autoComplete="off"
        placeholder="dd/mm/aaaa"
        value={texto}
        onChange={handleChange}
        required={required}
        disabled={disabled}
        id={id}
        title={title}
        aria-label={ariaLabel}
        className={["date-input", className].filter(Boolean).join(" ")}
        maxLength={10}
      />
      <button
        ref={botaoRef}
        type="button"
        className="date-cal-btn"
        aria-label="Abrir calendário"
        title="Abrir calendário"
        aria-haspopup="dialog"
        aria-expanded={calendarioAberto}
        disabled={disabled}
        onClick={() => setAberto((a) => !a)}
      >
        <Icon name="cal" size={14} />
      </button>
      {calendarioAberto && (
        <Calendario
          modo="dia"
          valor={textoParaIso(texto) ?? ""}
          min={min}
          ancora={envolucro}
          aoEscolher={onChange}
          aoLimpar={() => onChange("")}
          aoFechar={fechar}
        />
      )}
    </span>
  );
}
