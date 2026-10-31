import { apiDelete, apiGet, apiPatch, apiPost, apiPut } from "./client";

/** Espelha SPI.Application.Common.Dtos.AlunoResumoResponse. */
export interface AlunoResumo {
  id: number;
  ra: string;
  nome: string;
}

/** Espelha SPI.Domain.Enums.TipoTurma (serializado como string). */
export type TipoTurma = "Mensalidade" | "Pacote";

/** Espelha SPI.Application.Turmas.Dtos.TurmaResponse. */
export interface Turma {
  id: number;
  nome: string;
  professorId: number;
  ativo: boolean;
  /** specs/063: Mensalidade (comum) ou Pacote (fechada: ninguem novo entra depois que comeca). */
  tipo: TipoTurma;
  /** Somente turma de Pacote (YYYY-MM-DD). Turma migrada pode estar sem datas. */
  dataInicio: string | null;
  dataFim: string | null;
  /** Calculado no servidor: turma de pacote ja iniciada. A tela so exibe; nao recalcula. */
  fechada: boolean;
  alunos: AlunoResumo[];
  /** Turma de pacote: quem saiu mas ainda tem o pacote ativo dela (so esses podem voltar pela matricula direta). */
  alunosComPacoteForaDaTurma: AlunoResumo[];
}

/** Espelha SPI.Application.Turmas.Dtos.TurmaRequest. */
export interface TurmaRequest {
  nome: string;
  /** Opcional: ausente no cadastro = Mensalidade; na edicao = mantem o tipo atual (nao muda). */
  tipo?: TipoTurma;
  /** Somente Pacote; obrigatorias no cadastro. A data de inicio nao muda depois de preenchida. */
  dataInicio?: string | null;
  dataFim?: string | null;
}

export function listarTurmas(accessToken: string, filtros?: { nome?: string; ativo?: boolean }): Promise<Turma[]> {
  return apiGet<Turma[]>("/api/turmas", accessToken, filtros);
}

export function obterTurma(id: number, accessToken: string): Promise<Turma> {
  return apiGet<Turma>(`/api/turmas/${id}`, accessToken);
}

export function cadastrarTurma(request: TurmaRequest, accessToken: string): Promise<Turma> {
  return apiPost<Turma>("/api/turmas", request, accessToken);
}

export function atualizarTurma(id: number, request: TurmaRequest, accessToken: string): Promise<Turma> {
  return apiPut<Turma>(`/api/turmas/${id}`, request, accessToken);
}

export function excluirTurma(id: number, accessToken: string): Promise<void> {
  return apiDelete(`/api/turmas/${id}`, accessToken);
}

export function reativarTurma(id: number, accessToken: string): Promise<Turma> {
  return apiPatch<Turma>(`/api/turmas/${id}/reativar`, accessToken);
}

export function vincularAluno(turmaId: number, alunoId: number, accessToken: string): Promise<Turma> {
  return apiPost<Turma>(`/api/turmas/${turmaId}/alunos/${alunoId}`, {}, accessToken);
}

export function desvincularAluno(turmaId: number, alunoId: number, accessToken: string): Promise<void> {
  return apiDelete(`/api/turmas/${turmaId}/alunos/${alunoId}`, accessToken);
}
