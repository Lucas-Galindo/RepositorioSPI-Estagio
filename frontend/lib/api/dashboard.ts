import { apiGet } from "./client";
import type { ProgressoPacote } from "./vinculosCobranca";

/** Espelha SPI.Application.Dashboard.Dtos.GargaloCaixaResponse. */
export interface GargaloCaixa {
  diaMaiorEntrada: number | null;
  valorMaiorEntrada: number;
  diaMaiorSaida: number | null;
  valorMaiorSaida: number;
}

/** Espelha SPI.Application.Dashboard.Dtos.IndicadoresFinanceirosResponse. */
export interface IndicadoresFinanceiros {
  taxaInadimplenciaPercentual: number;
  prazoMedioAtrasoDias: number | null;
  fluxoCaixaOperacional: number;
  gargaloCaixa: GargaloCaixa;
}

/** Espelha SPI.Application.Dashboard.Dtos.FluxoCaixaMensalItem. */
export interface FluxoCaixaMensalItem {
  ano: number;
  mes: number;
  entradas: number;
  saidas: number;
  saldo: number;
}

/** Espelha SPI.Application.Relatorios.Dtos.LancamentoIndicadorItem. */
export interface LancamentoIndicador {
  id: number;
  tipo: "Entrada" | "Saida";
  descricao: string;
  dataVencimento: string;
  valor: number;
  status: string;
}

/** Espelha SPI.Application.Dashboard.Dtos.AulaResumoResponse. */
export interface AulaResumo {
  id: number;
  materiaNome: string;
  turmaNome: string | null;
  dataInicio: string;
  horaInicio: string;
  horaFim: string;
  status: string;
}

/** Espelha SPI.Application.Dashboard.Dtos.PacoteEmAtencaoResponse (specs/058). */
export interface PacoteEmAtencao {
  vinculoId: number;
  alunoId: number;
  alunoNome: string;
  progresso: ProgressoPacote;
}

/** Espelha SPI.Application.Dashboard.Dtos.PacotesResumoResponse (specs/058). */
export interface PacotesResumo {
  ativos: number;
  concluidos: number;
  proximosDoFim: number;
  aulasDadas: number;
  aulasRestantes: number;
  aulasDadasNoPeriodo: number;
  /** Ja ordenada no backend (esgotados primeiro). */
  renovacao: PacoteEmAtencao[];
}

/** Espelha SPI.Application.Dashboard.Dtos.DashboardResponse. */
export interface Dashboard {
  totalAulasAgendadasNoPeriodo: number;
  totalAlunosAtivos: number;
  alunosAtendidosNoPeriodo: number;
  totalTurmasAtivas: number;
  valorPendenteRecebimento: number;
  valorFaturadoNoPeriodo: number;
  proximasAulasHoje: AulaResumo[];
  lembretesPendentes: number;
  pacotes: PacotesResumo;
}

export function obterDashboard(
  accessToken: string,
  filtros?: { periodoInicio?: string; periodoFim?: string; alunoId?: number }
): Promise<Dashboard> {
  return apiGet<Dashboard>("/api/dashboard", accessToken, filtros);
}
