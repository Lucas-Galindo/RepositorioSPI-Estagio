import { apiGet, apiPost, apiPut } from "./client";

/** Espelha SPI.Application.ContasPagar.Dtos.ContaPagarResponse. */
export interface ContaPagar {
  id: number;
  descricao: string;
  categoriaDespesaId: number;
  categoriaDespesaNome: string;
  favorecido: string | null;
  valor: number;
  competencia: string | null;
  dataVencimento: string;
  dataPagamento: string | null;
  formaPagamentoId: number | null;
  formaPagamentoNome: string | null;
  observacoes: string | null;
  status: "Pendente" | "Pago" | "Atrasado" | "Cancelado";
}

/** Espelha SPI.Application.ContasPagar.Dtos.RegistrarContaPagarRequest. */
export interface RegistrarContaPagarRequest {
  descricao: string;
  categoriaDespesaId: number;
  favorecido?: string | null;
  valor: number;
  competencia?: string | null;
  dataVencimento: string;
  formaPagamentoId?: number | null;
  observacoes?: string | null;
  status: "Pendente" | "Pago";
}

/** Espelha SPI.Application.ContasPagar.Dtos.AtualizarContaPagarRequest. */
export interface AtualizarContaPagarRequest {
  descricao?: string | null;
  categoriaDespesaId?: number | null;
  favorecido?: string | null;
  valor?: number | null;
  competencia?: string | null;
  dataVencimento?: string | null;
  formaPagamentoId?: number | null;
  observacoes?: string | null;
}

export interface CategoriaDespesa {
  id: number;
  nome: string;
}

export function listarContasPagar(
  accessToken: string,
  filtros?: { categoriaDespesaId?: number; status?: string; favorecido?: string; vencimentoInicio?: string; vencimentoFim?: string }
): Promise<ContaPagar[]> {
  return apiGet<ContaPagar[]>("/api/contas-pagar", accessToken, filtros);
}

export function obterContaPagar(id: number, accessToken: string): Promise<ContaPagar> {
  return apiGet<ContaPagar>(`/api/contas-pagar/${id}`, accessToken);
}

export function registrarContaPagar(request: RegistrarContaPagarRequest, accessToken: string): Promise<ContaPagar> {
  return apiPost<ContaPagar>("/api/contas-pagar", request, accessToken);
}

export function editarContaPagar(id: number, request: AtualizarContaPagarRequest, accessToken: string): Promise<ContaPagar> {
  return apiPut<ContaPagar>(`/api/contas-pagar/${id}`, request, accessToken);
}

export function atualizarStatusContaPagar(
  id: number,
  status: "Pendente" | "Pago" | "Atrasado" | "Cancelado",
  accessToken: string
): Promise<ContaPagar> {
  return apiPut<ContaPagar>(`/api/contas-pagar/${id}/status`, { status }, accessToken);
}

export function listarCategoriasDespesa(accessToken: string): Promise<CategoriaDespesa[]> {
  return apiGet<CategoriaDespesa[]>("/api/categorias-despesa", accessToken);
}
