import { apiGet } from "./client";
import type { IndicadoresFinanceiros, FluxoCaixaMensalItem } from "./dashboard";

/** Espelha SPI.Application.Relatorios.Dtos.RelatorioFinanceiroResponse. */
export interface RelatorioFinanceiro {
  totalRecebido: number;
  totalPago: number;
  saldoRealizado: number;
  receitaPendente: number;
  despesaPendente: number;
  saldoPrevisto: number;
  porFormaPagamento: { chave: string; total: number }[];
  porAluno: { chave: string; total: number }[];
  porTurma: { chave: string; total: number }[];
}

export function obterRelatorioFinanceiro(
  accessToken: string,
  filtros?: { inicio?: string; fim?: string; formaPagamentoId?: number; alunoId?: number; turmaId?: number; materiaId?: number }
): Promise<RelatorioFinanceiro> {
  return apiGet<RelatorioFinanceiro>("/api/relatorios/financeiro", accessToken, filtros);
}

/** Espelha SPI.Application.Relatorios.Dtos.IndicadoresFinanceirosFiltradosResponse. */
export interface IndicadoresFinanceirosFiltrados {
  indicadores: IndicadoresFinanceiros;
  fluxoCaixaMensal: FluxoCaixaMensalItem[];
}

export function obterIndicadoresFinanceiros(
  accessToken: string,
  filtros?: { inicio?: string; fim?: string; turmaId?: number; materiaId?: number; alunoId?: number }
): Promise<IndicadoresFinanceirosFiltrados> {
  return apiGet<IndicadoresFinanceirosFiltrados>("/api/relatorios/indicadores-financeiros", accessToken, filtros);
}
