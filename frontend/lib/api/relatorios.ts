import { apiGet } from "./client";

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
  filtros?: { inicio?: string; fim?: string; formaPagamentoId?: number; alunoId?: number; turmaId?: number }
): Promise<RelatorioFinanceiro> {
  return apiGet<RelatorioFinanceiro>("/api/relatorios/financeiro", accessToken, filtros);
}
