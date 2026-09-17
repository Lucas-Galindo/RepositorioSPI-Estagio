"use client";

import { useState } from "react";

function hhmmParaTexto(hhmm: string): string {
  if (!hhmm) return "";
  return hhmm.slice(0, 5);
}

function mascarar(digitos: string): string {
  const h = digitos.slice(0, 2);
  const m = digitos.slice(2, 4);
  let texto = h;
  if (m) texto += `:${m}`;
  return texto;
}

function textoParaHhmm(texto: string): string | null {
  const digitos = texto.replace(/\D/g, "");
  if (digitos.length !== 4) return null;
  const h = digitos.slice(0, 2);
  const m = digitos.slice(2, 4);
  const hora = Number(h);
  const minuto = Number(m);
  if (hora > 23 || minuto > 59) return null;
  return `${h}:${m}`;
}

interface TimeInputProps {
  value: string;
  onChange: (value: string) => void;
  required?: boolean;
  id?: string;
  className?: string;
  title?: string;
}

/**
 * Campo de horario mascarado HH:mm (24h) — substitui `<input type="time">` nativo para que o
 * formato exibido/aceito nao dependa do idioma/SO do navegador da usuaria (nunca AM/PM). O
 * valor emitido/recebido continua `HH:mm`, o mesmo contrato do input nativo que substitui.
 */
export function TimeInput({ value, onChange, required, id, className, title }: TimeInputProps) {
  const [texto, setTexto] = useState(() => hhmmParaTexto(value));
  const [ultimoValue, setUltimoValue] = useState(value);

  // Ajusta o texto exibido quando o valor muda por fora (ex: reset de formulario),
  // sem useEffect — segue o padrao recomendado pelo React para "adjusting state
  // when a prop changes" (evita o set-state-in-effect / cascading renders).
  if (value !== ultimoValue) {
    setUltimoValue(value);
    setTexto(hhmmParaTexto(value));
  }

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const digitos = e.target.value.replace(/\D/g, "").slice(0, 4);
    const mascarado = mascarar(digitos);
    setTexto(mascarado);
    if (digitos.length === 0) {
      onChange("");
      return;
    }
    const hhmm = textoParaHhmm(mascarado);
    if (hhmm) onChange(hhmm);
  }

  return (
    <input
      type="text"
      inputMode="numeric"
      autoComplete="off"
      placeholder="HH:mm"
      value={texto}
      onChange={handleChange}
      required={required}
      id={id}
      title={title}
      className={["time-input", className].filter(Boolean).join(" ")}
      maxLength={5}
    />
  );
}
