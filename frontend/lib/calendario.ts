// specs/070: logica pura do calendario proprio. Sem React e sem `Date`: dia da semana e tamanho do mes saem de
// aritmetica, e as datas sao texto `yyyy-MM-dd` / `yyyy-MM` (a comparacao de texto nesse formato ja ordena as
// datas). Assim o fuso/horario de verao do navegador nunca desloca o dia mostrado (FR-008).
// Node 24 executa este .ts direto (scripts/verificar-calendario.mjs), entao nada aqui usa recurso de TypeScript
// que precise de transformacao (enum, parameter properties).

export const ANO_MIN = 1900;
export const ANO_MAX = 2100;

export const MESES = [
  "janeiro", "fevereiro", "março", "abril", "maio", "junho",
  "julho", "agosto", "setembro", "outubro", "novembro", "dezembro",
] as const;

export const MESES_CURTOS = [
  "jan", "fev", "mar", "abr", "mai", "jun",
  "jul", "ago", "set", "out", "nov", "dez",
] as const;

/** Domingo primeiro. */
export const DIAS_ABREVIADOS = ["D", "S", "T", "Q", "Q", "S", "S"] as const;

export interface PartesDeData {
  ano: number;
  mes: number;
  dia: number;
}

export interface CelulaDoDia {
  iso: string;
  dia: number;
  doMes: boolean;
}

function dois(n: number): string {
  return n < 10 ? `0${n}` : String(n);
}

function quatro(n: number): string {
  return String(n).padStart(4, "0");
}

export function montarIso(ano: number, mes: number, dia: number): string {
  return `${quatro(ano)}-${dois(mes)}-${dois(dia)}`;
}

export function montarMes(ano: number, mes: number): string {
  return `${quatro(ano)}-${dois(mes)}`;
}

export function ehBissexto(ano: number): boolean {
  return (ano % 4 === 0 && ano % 100 !== 0) || ano % 400 === 0;
}

/** `mes` de 1 a 12. */
export function diasDoMes(ano: number, mes: number): number {
  if (mes === 2) return ehBissexto(ano) ? 29 : 28;
  return mes === 4 || mes === 6 || mes === 9 || mes === 11 ? 30 : 31;
}

const SAKAMOTO = [0, 3, 2, 5, 0, 3, 5, 1, 4, 6, 2, 4];

/** 0 (domingo) a 6 (sabado), formula de Sakamoto. */
export function diaDaSemana(ano: number, mes: number, dia: number): number {
  const a = mes < 3 ? ano - 1 : ano;
  return (a + Math.floor(a / 4) - Math.floor(a / 100) + Math.floor(a / 400) + SAKAMOTO[mes - 1] + dia) % 7;
}

/** `yyyy-MM-dd` valido (formato, mes 1 a 12, dia existente, ano de ANO_MIN a ANO_MAX) ou `null`. */
export function lerIso(iso: string): PartesDeData | null {
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(iso);
  if (!m) return null;
  const ano = Number(m[1]);
  const mes = Number(m[2]);
  const dia = Number(m[3]);
  if (ano < ANO_MIN || ano > ANO_MAX || mes < 1 || mes > 12) return null;
  if (dia < 1 || dia > diasDoMes(ano, mes)) return null;
  return { ano, mes, dia };
}

/** `yyyy-MM` valido ou `null`. */
export function lerMes(aaaaMm: string): { ano: number; mes: number } | null {
  const m = /^(\d{4})-(\d{2})$/.exec(aaaaMm);
  if (!m) return null;
  const ano = Number(m[1]);
  const mes = Number(m[2]);
  if (ano < ANO_MIN || ano > ANO_MAX || mes < 1 || mes > 12) return null;
  return { ano, mes };
}

/** Navegacao entre meses com virada de ano. `mes` de 1 a 12. */
export function adicionarMeses(ano: number, mes: number, delta: number): { ano: number; mes: number } {
  const indice = ano * 12 + (mes - 1) + delta;
  return { ano: Math.floor(indice / 12), mes: (((indice % 12) + 12) % 12) + 1 };
}

/** Soma dias a um `yyyy-MM-dd` (negativo volta), virando mes e ano. Nao valida a faixa de anos. */
export function adicionarDias(iso: string, delta: number): string {
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(iso);
  if (!m) return iso;
  let ano = Number(m[1]);
  let mes = Number(m[2]);
  let dia = Number(m[3]);
  let restante = delta;
  while (restante > 0) {
    if (dia < diasDoMes(ano, mes)) {
      dia++;
    } else {
      ({ ano, mes } = adicionarMeses(ano, mes, 1));
      dia = 1;
    }
    restante--;
  }
  while (restante < 0) {
    if (dia > 1) {
      dia--;
    } else {
      ({ ano, mes } = adicionarMeses(ano, mes, -1));
      dia = diasDoMes(ano, mes);
    }
    restante++;
  }
  return montarIso(ano, mes, dia);
}

/** Dia fora do intervalo [min, max] (comparacao de texto `yyyy-MM-dd`). Limite vazio = sem limite. */
export function estaBloqueado(iso: string, min?: string, max?: string): boolean {
  return (!!min && iso < min) || (!!max && iso > max);
}

/** 6 semanas x 7 dias, domingo primeiro; dias dos meses vizinhos vem com `doMes: false`. */
export function montarGrade(ano: number, mes: number): CelulaDoDia[][] {
  const antes = diaDaSemana(ano, mes, 1);
  const inicio = adicionarDias(montarIso(ano, mes, 1), -antes);
  const semanas: CelulaDoDia[][] = [];
  let atual = inicio;
  const mesTexto = montarMes(ano, mes);
  for (let s = 0; s < 6; s++) {
    const semana: CelulaDoDia[] = [];
    for (let d = 0; d < 7; d++) {
      semana.push({ iso: atual, dia: Number(atual.slice(8, 10)), doMes: atual.startsWith(mesTexto) });
      atual = adicionarDias(atual, 1);
    }
    semanas.push(semana);
  }
  return semanas;
}

/** "novembro de 2026". */
export function tituloDoMes(ano: number, mes: number): string {
  return `${MESES[mes - 1]} de ${ano}`;
}

/** "5 de novembro de 2026", para leitor de tela. */
export function rotuloDoDia(iso: string): string {
  const p = lerIso(iso);
  if (!p) return iso;
  return `${p.dia} de ${MESES[p.mes - 1]} de ${p.ano}`;
}

/** 12 anos da pagina (blocos de 12 a partir de ANO_MIN) que contem `ano`, limitados a ANO_MIN..ANO_MAX. */
export function anosDaPagina(ano: number): number[] {
  const preso = Math.min(Math.max(ano, ANO_MIN), ANO_MAX);
  const inicio = ANO_MIN + Math.floor((preso - ANO_MIN) / 12) * 12;
  const anos: number[] = [];
  for (let a = inicio; a < inicio + 12 && a <= ANO_MAX; a++) anos.push(a);
  return anos;
}
