import { apiGet } from "./client";

/** Espelha SPI.Application.Financeiro.Dtos.VisaoGeralFinanceiraResponse. */
export interface VisaoGeralFinanceira {
  receitas: { recebido: number; aReceber: number; atrasado: number };
  despesas: { pago: number; aPagar: number; atrasado: number };
  resultado: { saldoRealizado: number; saldoPrevisto: number };
  proximosVencimentos: {
    tipo: "Receber" | "Pagar";
    descricao: string;
    dataVencimento: string;
    valor: number;
  }[];
}

export function obterVisaoGeralFinanceira(
  accessToken: string,
  filtros?: { periodoInicio?: string; periodoFim?: string }
): Promise<VisaoGeralFinanceira> {
  return apiGet<VisaoGeralFinanceira>("/api/financeiro/visao-geral", accessToken, filtros);
}
