import { apiDelete, apiGet, apiPatch, apiPost, apiPut } from "./client";

/** Espelha SPI.Domain.Enums.ModalidadeCobranca (serializado como string). */
export type ModalidadeCobranca = "Avulsa" | "Mensalidade" | "Pacote";

/** Espelha SPI.Application.VinculosCobranca.Dtos.VinculoCobrancaResponse. */
export interface VinculoCobranca {
  id: number;
  alunoId: number;
  turmaId: number | null;
  /** Nulo quando turmaId e nulo (atendimento individual). */
  turmaNome: string | null;
  modalidade: ModalidadeCobranca;
  valor: number;
  aulasIncluidas: number | null;
  saldoAulas: number | null;
  ativo: boolean;
}

/** Espelha SPI.Application.VinculosCobranca.Dtos.VinculoCobrancaRequest. */
export interface VinculoCobrancaRequest {
  /** Nulo = atendimento individual. */
  turmaId: number | null;
  modalidade: ModalidadeCobranca;
  valor: number;
  /** Somente com Mensalidade. */
  aulasIncluidas: number | null;
  /** Somente com Pacote. */
  saldoAulas: number | null;
}

const base = (alunoId: number) => `/api/alunos/${alunoId}/vinculos-cobranca`;

export function listarVinculosCobranca(
  alunoId: number,
  accessToken: string,
  filtros?: { ativo?: boolean }
): Promise<VinculoCobranca[]> {
  return apiGet<VinculoCobranca[]>(base(alunoId), accessToken, filtros);
}

export function cadastrarVinculoCobranca(
  alunoId: number,
  request: VinculoCobrancaRequest,
  accessToken: string
): Promise<VinculoCobranca> {
  return apiPost<VinculoCobranca>(base(alunoId), request, accessToken);
}

export function atualizarVinculoCobranca(
  alunoId: number,
  id: number,
  request: VinculoCobrancaRequest,
  accessToken: string
): Promise<VinculoCobranca> {
  return apiPut<VinculoCobranca>(`${base(alunoId)}/${id}`, request, accessToken);
}

export function excluirVinculoCobranca(alunoId: number, id: number, accessToken: string): Promise<void> {
  return apiDelete(`${base(alunoId)}/${id}`, accessToken);
}

export function reativarVinculoCobranca(alunoId: number, id: number, accessToken: string): Promise<VinculoCobranca> {
  return apiPatch<VinculoCobranca>(`${base(alunoId)}/${id}/reativar`, accessToken);
}
