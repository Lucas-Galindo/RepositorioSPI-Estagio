/** Espelha os helpers currency/initials/avatarColor/fmtData do protótipo. */

export function currency(valor: number): string {
  return "R$ " + valor.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export function initials(nome: string): string {
  return nome
    .split(" ")
    .filter((palavra) => palavra.length > 1)
    .slice(0, 2)
    .map((palavra) => palavra[0])
    .join("")
    .toUpperCase();
}

const AV_COLORS = ["var(--c-accent-deep)", "var(--c-accent2)", "var(--c-sidebar)"];

export function avatarColor(id: string | number): string {
  let hash = 0;
  for (const char of String(id)) {
    hash += char.charCodeAt(0);
  }
  return AV_COLORS[hash % AV_COLORS.length];
}

/** Formata uma data ISO (yyyy-MM-dd) para dd/mm/aaaa. */
export function fmtData(iso: string): string {
  const [y, m, d] = iso.split("-");
  return `${d}/${m}/${y}`;
}

/** Formata "HH:mm:ss" (TimeOnly do backend) para "HH:mm". */
export function fmtHora(hora: string): string {
  return hora.slice(0, 5);
}

/** Formata um DateTime ISO (ex: "2026-09-10T14:00:00") para "10/09/2026 14:00". */
export function fmtDataHora(iso: string): string {
  const [data, hora] = iso.split("T");
  return `${fmtData(data)} ${(hora ?? "").slice(0, 5)}`;
}

const MESES = [
  "janeiro", "fevereiro", "março", "abril", "maio", "junho",
  "julho", "agosto", "setembro", "outubro", "novembro", "dezembro",
];

/**
 * Formata uma data ISO (yyyy-MM-dd, ex: competencia) para "mês de aaaa".
 * Faz parsing manual da string (mesma tecnica de fmtData) em vez de usar
 * `new Date(iso)`: o construtor de Date interpreta yyyy-MM-dd como UTC
 * meia-noite e, ao formatar no fuso local, pode exibir o mes anterior.
 */
export function fmtMesAno(iso: string): string {
  const [y, m] = iso.split("-");
  return `${MESES[Number(m) - 1]} de ${y}`;
}

const MESES_ABREV = ["Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez"];

/** Formata ano/mes numericos (ex: 2026, 9) para "Set/2026", para tabelas compactas. */
export function fmtMesAbreviado(ano: number, mes: number): string {
  return `${MESES_ABREV[mes - 1]}/${ano}`;
}
