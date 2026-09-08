import { apiDelete, apiGet, apiPost, apiPut } from "./client";

/** Espelha SPI.Application.Common.Dtos.AlunoResumoResponse. */
export interface AlunoResumo {
  id: number;
  ra: string;
  nome: string;
}

/** Espelha SPI.Application.Turmas.Dtos.TurmaResponse. */
export interface Turma {
  id: number;
  nome: string;
  professorId: number;
  ativo: boolean;
  alunos: AlunoResumo[];
}

/** Espelha SPI.Application.Turmas.Dtos.TurmaRequest. */
export interface TurmaRequest {
  nome: string;
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

export function vincularAluno(turmaId: number, alunoId: number, accessToken: string): Promise<Turma> {
  return apiPost<Turma>(`/api/turmas/${turmaId}/alunos/${alunoId}`, {}, accessToken);
}

export function desvincularAluno(turmaId: number, alunoId: number, accessToken: string): Promise<void> {
  return apiDelete(`/api/turmas/${turmaId}/alunos/${alunoId}`, accessToken);
}
