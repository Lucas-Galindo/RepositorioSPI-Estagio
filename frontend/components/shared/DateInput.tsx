"use client";

import { useState } from "react";

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
}

/**
 * Campo de data mascarado dd/mm/aaaa — substitui `<input type="date">` nativo para que o
 * formato exibido/aceito nao dependa do idioma/SO do navegador da usuaria. O valor
 * emitido/recebido continua `yyyy-MM-dd`, o mesmo contrato do input nativo que substitui.
 */
export function DateInput({ value, onChange, required, id, className, title }: DateInputProps) {
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

  return (
    <input
      type="text"
      inputMode="numeric"
      autoComplete="off"
      placeholder="dd/mm/aaaa"
      value={texto}
      onChange={handleChange}
      required={required}
      id={id}
      title={title}
      className={["date-input", className].filter(Boolean).join(" ")}
      maxLength={10}
    />
  );
}
