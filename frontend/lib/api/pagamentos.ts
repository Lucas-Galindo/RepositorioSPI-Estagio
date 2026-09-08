import { apiGet, apiPost, apiPut } from "./client";

/** Espelha SPI.Application.Pagamentos.Dtos.PagamentoResponse (Contas a Receber). */
export interface Pagamento {
  id: number;
  alunoId: number;
  alunoNome: string;
  descricao: string | null;
  categoriaReceitaId: number | null;
  categoriaReceitaNome: string | null;
  formaPagamentoId: number | null;
  formaPagamentoNome: string | null;
  dataVencimento: string;
  dataPagamento: string | null;
  competencia: string | null;
  valorFinal: number;
  observacoes: string | null;
  status: "Pendente" | "Pago" | "Atrasado" | "Cancelado";
  aulaIds: number[];
}

/** Espelha SPI.Application.Pagamentos.Dtos.RegistrarPagamentoRequest. */
export interface RegistrarPagamentoRequest {
  alunoId: number;
  descricao?: string | null;
  categoriaReceitaId?: number | null;
  aulaIds: number[];
  dataVencimento: string;
  competencia?: string | null;
  valorFinal: number;
  formaPagamentoId: number;
  observacoes?: string | null;
  status: "Pendente" | "Pago";
}

/** Espelha SPI.Application.Pagamentos.Dtos.AtualizarPagamentoRequest. */
export interface AtualizarPagamentoRequest {
  descricao?: string | null;
  categoriaReceitaId?: number | null;
  formaPagamentoId?: number | null;
  dataVencimento?: string | null;
  valorFinal?: number | null;
  competencia?: string | null;
  observacoes?: string | null;
}

export interface FormaPagamento {
  id: number;
  forma: string;
  descricao: string | null;
}

export interface CategoriaReceita {
  id: number;
  nome: string;
}

export function listarPagamentos(
  accessToken: string,
  filtros?: { alunoId?: number; status?: string; vencimentoInicio?: string; vencimentoFim?: string }
): Promise<Pagamento[]> {
  return apiGet<Pagamento[]>("/api/pagamentos", accessToken, filtros);
}

export function obterPagamento(id: number, accessToken: string): Promise<Pagamento> {
  return apiGet<Pagamento>(`/api/pagamentos/${id}`, accessToken);
}

export function registrarPagamento(request: RegistrarPagamentoRequest, accessToken: string): Promise<Pagamento> {
  return apiPost<Pagamento>("/api/pagamentos", request, accessToken);
}

export function editarPagamento(id: number, request: AtualizarPagamentoRequest, accessToken: string): Promise<Pagamento> {
  return apiPut<Pagamento>(`/api/pagamentos/${id}`, request, accessToken);
}

export function atualizarStatusPagamento(
  id: number,
  status: "Pendente" | "Pago" | "Atrasado" | "Cancelado",
  accessToken: string
): Promise<Pagamento> {
  return apiPut<Pagamento>(`/api/pagamentos/${id}/status`, { status }, accessToken);
}

export function listarFormasPagamento(accessToken: string): Promise<FormaPagamento[]> {
  return apiGet<FormaPagamento[]>("/api/formas-pagamento", accessToken);
}

export function listarCategoriasReceita(accessToken: string): Promise<CategoriaReceita[]> {
  return apiGet<CategoriaReceita[]>("/api/categorias-receita", accessToken);
}
